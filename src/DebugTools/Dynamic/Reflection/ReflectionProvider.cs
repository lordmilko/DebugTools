using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DebugTools.Dynamic
{
    class ReflectionProvider
    {
        public static MethodInfoAndParameterRanks? FindBestMethod(MethodInfoAndParameters[] candidates, Type[] parameterTypes)
        {
            if (candidates.Length == 1)
            {
                var item = candidates[0];
                return new MethodInfoAndParameterRanks(item.Method, item.Parameters.Select(v => new ParameterInfoAndRank(v, null)).ToArray());
            }

            var matches = new List<MethodInfoAndParameterRanks>();

            foreach (var candidate in candidates)
            {
                var bad = false;

                //If the method doesn't expect any parameters at all but some where specified, we've already failed
                if (candidate.Parameters.Length == 0 && parameterTypes.Length > 0)
                    continue;

                var parameterRanks = new List<ParameterInfoAndRank>();

                for (var i = 0; i < candidate.Parameters.Length; i++)
                {
                    var parameter = candidate.Parameters[i];

                    var isParams = parameter.GetCustomAttribute<ParamArrayAttribute>() != null;

                    if (i < parameterTypes.Length)
                    {
                        var userType = parameterTypes[i];

                        if (isParams)
                        {
                            var result = IsAssignableTo(parameter.ParameterType.GetElementType(), userType);

                            if (result == ConversionRank.None)
                            {
                                bad = true;
                                break;
                            }
                            else
                                parameterRanks.Add(new ParameterInfoAndRank(parameter, result));
                        }
                        else
                        {
                            var result = IsAssignableTo(parameter.ParameterType, userType);

                            if (result == ConversionRank.None)
                            {
                                bad = true;
                                break;
                            }
                            else
                                parameterRanks.Add(new ParameterInfoAndRank(parameter, result));
                        }
                    }
                    else
                    {
                        //There's more parameters defined on the method than there were arguments. All these remaining parameters better be optional!

                        if (!parameter.HasDefaultValue && !isParams)
                        {
                            bad = true;
                            break;
                        }
                        else
                            parameterRanks.Add(new ParameterInfoAndRank(parameter, ConversionRank.None));
                    }
                }

                if (!bad)
                    matches.Add(new MethodInfoAndParameterRanks(candidate.Method, parameterRanks.ToArray()));
            }

            if (matches.Count == 0)
                return null;

            if (matches.Count == 1)
                return matches[0];

            var matchesWithScores = matches.Select(v => new
            {
                Match = v,
                Score = v.Parameters.Sum(a => (int) a.Rank)
            }).OrderByDescending(v => v.Score).ToArray();

            var topScore = matchesWithScores[0].Score;

            var withTopScore = matchesWithScores.Where(v => v.Score == topScore).ToArray();

            if (withTopScore.Length == 1)
                return withTopScore[0].Match;

            if (matchesWithScores.All(m => m.Score == topScore))
            {
                //All methods are equally good (or bad). Are we potentially trying to disambiguate between
                //IEnumerable.GetEnumerator() and IEnumerable<T>.GetEnumerator()?
                if (TryGetInterfaceOverride(matches, out var result))
                    return result;
            }

            throw new NotImplementedException("Disambiguating between multiple potentially valid overloads is not implemented");
        }

        public static bool TryGetInterfaceOverride<T>(List<T> matches, out T result) where T : IMethodMatcher
        {
            var ifaceMethods = new List<Tuple<T, MethodInfo>>();

            foreach (var match in matches)
            {
                var type = match.Method.DeclaringType;
                var ifaces = type.GetInterfaces();

                var found = false;

                foreach (var iface in ifaces)
                {
                    var ifaceMap = type.GetInterfaceMap(iface);

                    for (var i = 0; i < ifaceMap.TargetMethods.Length; i++)
                    {
                        if (ifaceMap.TargetMethods[i] == match.Method)
                        {
                            found = true;
                            ifaceMethods.Add(Tuple.Create(match, ifaceMap.InterfaceMethods[i]));
                            break;
                        }
                    }

                    if (found)
                        break;
                }

                if (!found)
                {
                    result = default;
                    return false;
                }
            }

            HashSet<Type> GetHierarchy(Type parentType, HashSet<Type> types)
            {
                if (types.Add(parentType))
                {
                    var childIfaces = parentType.GetInterfaces();

                    foreach (var iface in childIfaces)
                        GetHierarchy(iface, types);
                }

                return types;
            }

            var ifaceCollections = ifaceMethods.Select(m => new
            {
                Method = m,
                InterfaceHierarchy = GetHierarchy(m.Item2.DeclaringType, new HashSet<Type>())
            }).ToArray();

            //If one interface contains all other interfaces, we'll say it "supersedes" the others, and therefore wins

            var best = new List<T>();

            foreach (var outerCollection in ifaceCollections)
            {
                bool fail = false;

                foreach (var innerCollection in ifaceCollections)
                {
                    if (outerCollection == innerCollection)
                        continue;

                    if (!outerCollection.InterfaceHierarchy.Contains(innerCollection.Method.Item2.DeclaringType))
                    {
                        fail = true;
                        break;
                    }
                }

                if (!fail)
                    best.Add(outerCollection.Method.Item1);
            }

            if (best.Count == 1)
            {
                result = best[0];
                return true;
            }

            result = default;
            return false;
        }

        public static IndexerAndParameters? GetBestIndexer(ReflectionCache data, object[] indexes)
        {
            var matches = data.Indexers.Where(v => MatchesIndexes(v.Parameters, indexes)).ToArray();

            if (matches.Length == 0)
                return null;

            if (matches.Length == 1)
                return matches[0];

            if (typeof(IList).IsAssignableFrom(data.Type))
            {
                var ifaceMap = data.Type.GetInterfaceMap(typeof(IList));

                var nonIListMatches = matches.Where(m => !ifaceMap.TargetMethods.Contains(m.GetIndex)).ToArray();

                if (nonIListMatches.Length == 1)
                    return nonIListMatches[0];

                var listTTypes = data.Type.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>)).ToArray();

                var targetMethods = listTTypes.Select(data.Type.GetInterfaceMap).SelectMany(m => m.TargetMethods).ToArray();

                var nonIListTMatches = nonIListMatches.Where(m => !targetMethods.Contains(m.GetIndex)).ToArray();

                if (nonIListTMatches.Length == 1)
                    return nonIListTMatches[0];
            }

            if (typeof(IDictionary).IsAssignableFrom(data.Type))
            {
                var ifaceMap = data.Type.GetInterfaceMap(typeof(IDictionary));

                var nonIDictionaryMatches = matches.Where(m => !ifaceMap.TargetMethods.Contains(m.GetIndex)).ToArray();

                if (nonIDictionaryMatches.Length == 1)
                    return nonIDictionaryMatches[0];
            }

            throw new NotImplementedException("Disambiguating between multiple potentially valid indexer overloads is not implemented");
        }

        private static bool MatchesIndexes(ParameterInfo[] parameters, object[] indexes)
        {
            if (parameters.Length != indexes.Length)
                return false;

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var value = indexes[i];

                var valueType = value?.GetType() ?? typeof(object);

                if (IsAssignableTo(parameter.ParameterType, valueType) == ConversionRank.None)
                    return false;
            }

            return true;
        }

        internal static ConversionRank IsAssignableTo(Type parameterType, Type valueType)
        {
            if (parameterType == valueType)
                return ConversionRank.Exact;

            if (parameterType.IsAssignableFrom(valueType))
                return ConversionRank.AssignableFrom;

            if (parameterType.IsEnum && valueType == typeof(int))
                return ConversionRank.EnumUnderlying;

            if (parameterType == typeof(string))
            {
                if (valueType.IsPrimitive || valueType.IsEnum)
                    return ConversionRank.SimpleToString;
            }

            if (valueType == typeof(string))
            {
                if (parameterType.IsEnum)
                    return ConversionRank.StringToEnum;

                if (parameterType.IsPrimitive)
                    return ConversionRank.StringToPrimitive;
            }

            if (!parameterType.IsInterface)
                return ConversionRank.None;

            //We know the parameter type is an interface; now check to see whether our value implements that interface

            var ifaces = valueType.GetInterfaces();

            foreach (var iface in ifaces)
            {
                if (iface == parameterType)
                    return ConversionRank.ImplementsInterface;
            }

            return ConversionRank.None;
        }
    }
}
