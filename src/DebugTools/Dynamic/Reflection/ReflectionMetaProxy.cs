using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace DebugTools.Dynamic
{
    partial class ReflectionProxy
    {
        internal class ReflectionMetaProxy : DynamicMetaProxy<ReflectionProxy>
        {
            private static ConcurrentDictionary<Type, ReflectionCache> cache = new ConcurrentDictionary<Type, ReflectionCache>();

            public static readonly ReflectionMetaProxy Instance = new ReflectionMetaProxy();

            private ReflectionCache GetData(ReflectionProxy instance) => cache.GetOrAdd(instance.Value.GetType(), t => new ReflectionCache(t));

            #region Index

            public override bool TryGetIndex(ReflectionProxy instance, GetIndexBinder binder, object[] indexes, out object value)
            {
                try
                {
                    var data = GetData(instance);

                    var match = GetBestIndexer(data, indexes);

                    if (match != null)
                    {
                        value = match.Value.GetIndex.Invoke(instance.Value, indexes);
                        return true;
                    }

                    value = null;
                    return false;
                }
                catch (TargetInvocationException ex)
                {
                    throw GetRemotableException(ex);
                }
            }

            public override bool TrySetIndex(ReflectionProxy instance, SetIndexBinder binder, object[] indexes, object value)
            {
                try
                {
                    var data = GetData(instance);

                    var match = GetBestIndexer(data, indexes);

                    if (match != null)
                    {
                        var parameters = new object[indexes.Length + 1];

                        for (var i = 0; i < indexes.Length; i++)
                            parameters[i] = indexes[i];

                        parameters[indexes.Length] = value;

                        match.Value.SetIndex.Invoke(instance.Value, parameters);
                        return true;
                    }

                    return false;
                }
                catch (TargetInvocationException ex)
                {
                    throw GetRemotableException(ex);
                }
            }

            private IndexerAndParameters? GetBestIndexer(ReflectionCache data, object[] indexes)
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
                }

                throw new NotImplementedException("Disambiguating between multiple potentially valid indexer overloads is not implemented");
            }

            #endregion
            #region Member

            public override bool TryGetMember(ReflectionProxy instance, GetMemberBinder binder, out object value)
            {
                try
                {
                    var data = GetData(instance);

                    if (data.Fields.TryGetValue(binder.Name, out var fieldInfo))
                    {
                        value = fieldInfo.GetValue(instance.Value);
                        return true;
                    }

                    if (data.Properties.TryGetValue(binder.Name, out var propertyInfo))
                    {
                        value = propertyInfo.GetValue(instance.Value);
                        return true;
                    }

                    if (binder.IgnoreCase)
                    {
                        if (data.FieldsIgnoreCase.TryGetValue(binder.Name, out fieldInfo))
                        {
                            value = fieldInfo.GetValue(instance.Value);
                            return true;
                        }

                        if (data.PropertiesIgnoreCase.TryGetValue(binder.Name, out propertyInfo))
                        {
                            value = propertyInfo.GetValue(instance.Value);
                            return true;
                        }
                    }

                    value = null;
                    return false;
                }
                catch (TargetInvocationException ex)
                {
                    throw GetRemotableException(ex);
                }
            }

            public override bool TrySetMember(ReflectionProxy instance, SetMemberBinder binder, object value)
            {
                try
                {
                    var data = GetData(instance);

                    if (data.Fields.TryGetValue(binder.Name, out var fieldInfo))
                    {
                        fieldInfo.SetValue(instance.Value, value);
                        return true;
                    }

                    if (data.Properties.TryGetValue(binder.Name, out var propertyInfo))
                    {
                        propertyInfo.SetValue(instance.Value, value);
                        return true;
                    }

                    if (binder.IgnoreCase)
                    {
                        if (data.FieldsIgnoreCase.TryGetValue(binder.Name, out fieldInfo))
                        {
                            fieldInfo.SetValue(instance.Value, value);
                            return true;
                        }

                        if (data.PropertiesIgnoreCase.TryGetValue(binder.Name, out propertyInfo))
                        {
                            propertyInfo.SetValue(instance.Value, value);
                            return true;
                        }
                    }

                    value = null;
                    return false;
                }
                catch (TargetInvocationException ex)
                {
                    throw GetRemotableException(ex);
                }
            }

            public override bool TryInvokeMember(ReflectionProxy instance, InvokeMemberBinder binder, object[] args, out object value)
            {
                try
                {
                    var data = GetData(instance);

                    var parameterTypes = args.Select(v => v?.GetType() ?? typeof(object)).ToArray();

                    if (data.Methods.TryGetValue(binder.Name, out var candidates))
                    {
                        var bestMethod = FindBestMethod(candidates, parameterTypes);

                        if (bestMethod != null)
                        {
                            args = BuildMethodInvocationArgs(bestMethod.Value, args);
                            value = bestMethod.Value.Method.Invoke(instance.Value, args);
                            return true;
                        }
                    }

                    if (binder.IgnoreCase)
                    {
                        if (data.MethodsIgnoreCase.TryGetValue(binder.Name, out candidates))
                        {
                            var bestMethod = FindBestMethod(candidates, parameterTypes);

                            if (bestMethod != null)
                            {
                                args = BuildMethodInvocationArgs(bestMethod.Value, args);
                                value = bestMethod.Value.Method.Invoke(instance.Value, args);
                                return true;
                            }
                        }
                    }

                    value = null;
                    return false;
                }
                catch (TargetInvocationException ex)
                {
                    throw GetRemotableException(ex);
                }
            }

            private object[] BuildMethodInvocationArgs(MethodInfoAndParameters info, object[] userArgs)
            {
                var outputArgs = new List<object>();

                for (var i = 0; i < info.Parameters.Length; i++)
                {
                    var parameter = info.Parameters[i];

                    var isParams = parameter.GetCustomAttribute<ParamArrayAttribute>() != null;

                    if (isParams)
                    {
                        var paramsArgs = userArgs.Skip(i).ToArray();

                        var paramsArray = Array.CreateInstance(parameter.ParameterType.GetElementType(), paramsArgs.Length);

                        if (paramsArgs.Length > 0)
                            Array.Copy(paramsArgs, paramsArray, paramsArgs.Length);

                        outputArgs.Add(paramsArray);
                        break;
                    }
                    else
                    {
                        if (i < userArgs.Length)
                            outputArgs.Add(userArgs[i]);
                        else
                        {
                            if (parameter.HasDefaultValue)
                                outputArgs.Add(Type.Missing);
                            else
                            {
                                throw new ArgumentException($"Expected a value for parameter '{parameter}' however none was specified.");
                            }
                        }
                    }
                }

                return outputArgs.ToArray();
            }

            #endregion

            private bool MatchesIndexes(ParameterInfo[] parameters, object[] indexes)
            {
                if (parameters.Length != indexes.Length)
                    return false;

                for (var i = 0; i < parameters.Length; i++)
                {
                    var parameter = parameters[i];
                    var value = indexes[i];

                    var valueType = value?.GetType() ?? typeof(object);

                    if (!IsAssignableTo(parameter.ParameterType, valueType))
                        return false;
                }

                return true;
            }

            private MethodInfoAndParameters? FindBestMethod(MethodInfoAndParameters[] candidates, Type[] parameterTypes)
            {
                if (candidates.Length == 1)
                    return candidates[0];

                var matches = new List<MethodInfoAndParameters>();

                foreach (var candidate in candidates)
                {
                    var bad = false;

                    //If the method doesn't expect any parameters at all but some where specified, we've already failed
                    if (candidate.Parameters.Length == 0 && parameterTypes.Length > 0)
                        continue;

                    for (var i = 0; i < candidate.Parameters.Length; i++)
                    {
                        var parameter = candidate.Parameters[i];

                        var isParams = parameter.GetCustomAttribute<ParamArrayAttribute>() != null;

                        if (i < parameterTypes.Length)
                        {
                            var userType = parameterTypes[i];

                            if (isParams)
                            {
                                if (!IsAssignableTo(parameter.ParameterType.GetElementType(), userType))
                                {
                                    bad = true;
                                    break;
                                }
                            }
                            else
                            {
                                if (!IsAssignableTo(parameter.ParameterType, userType))
                                {
                                    bad = true;
                                    break;
                                }
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
                        }
                    }

                    if (!bad)
                        matches.Add(candidate);
                }

                if (matches.Count == 0)
                    return null;

                if (matches.Count == 1)
                    return matches[0];

                throw new NotImplementedException("Disambiguating between multiple potentially valid overloads is not implemented");
            }

            private Exception GetRemotableException(Exception ex)
            {
                if (ex is TargetInvocationException e)
                    ex = ex.InnerException;

                if (Serialization.IsSerializable(ex))
                    return ex;

                return new RemoteException(ex.Message, ex.GetType());
            }

            private static bool IsAssignableTo(Type parameterType, Type valueType)
            {
                if (parameterType == valueType)
                    return true;

                if (parameterType.IsAssignableFrom(valueType))
                    return true;

                if (parameterType.IsEnum && valueType == typeof(int))
                    return true;

                if (!parameterType.IsInterface)
                    return false;

                var ifaces = valueType.GetInterfaces();

                foreach (var iface in ifaces)
                {
                    if (iface == parameterType)
                        return true;
                }

                return false;
            }

            public override IEnumerable<string> GetDynamicMemberNames(ReflectionProxy instance)
            {
                var data = GetData(instance);

                var list = new List<string>();

                if (!typeof(Array).IsAssignableFrom(data.Type))
                {
                    list.AddRange(data.Fields.Keys);
                    list.AddRange(data.Properties.Keys);
                }

                return list;
            }
        }
    }
}
