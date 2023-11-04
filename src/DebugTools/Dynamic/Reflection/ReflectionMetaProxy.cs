using System;
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

                    var match = ReflectionProvider.GetBestIndexer(data, indexes);

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

                    var match = ReflectionProvider.GetBestIndexer(data, indexes);

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
                        var bestMethod = ReflectionProvider.FindBestMethod(candidates, parameterTypes);

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
                            var bestMethod = ReflectionProvider.FindBestMethod(candidates, parameterTypes);

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

            private object[] BuildMethodInvocationArgs(MethodInfoAndParameterRanks info, object[] userArgs)
            {
                var outputArgs = new List<object>();

                for (var i = 0; i < info.Parameters.Length; i++)
                {
                    var parameter = info.Parameters[i];

                    var isParams = parameter.Info.GetCustomAttribute<ParamArrayAttribute>() != null;

                    if (isParams)
                    {
                        var paramsArgs = userArgs.Skip(i).ToArray();

                        var paramsArray = Array.CreateInstance(parameter.Info.ParameterType.GetElementType(), paramsArgs.Length);

                        if (paramsArgs.Length > 0)
                            Array.Copy(paramsArgs, paramsArray, paramsArgs.Length);

                        outputArgs.Add(paramsArray);
                        break;
                    }
                    else
                    {
                        if (i < userArgs.Length)
                            outputArgs.Add(ConvertValue(userArgs[i], parameter.Info.ParameterType, parameter.Rank));
                        else
                        {
                            if (parameter.Info.HasDefaultValue)
                                outputArgs.Add(Type.Missing);
                            else
                            {
                                throw new ArgumentException($"Expected a value for parameter '{parameter.Info}' however none was specified.");
                            }
                        }
                    }
                }

                return outputArgs.ToArray();
            }

            private object ConvertValue(object value, Type desiredType, ConversionRank? rank)
            {
                if (rank == null)
                {
                    //There was only 1 method overload
                    rank = ReflectionProvider.IsAssignableTo(desiredType, value?.GetType() ?? typeof(object));
                }

                switch (rank.Value)
                {
                    case ConversionRank.ImplementsInterface:
                    case ConversionRank.AssignableFrom:
                    case ConversionRank.Exact:
                        return value;
                    
                    case ConversionRank.SimpleToString:
                        return value.ToString();

                    case ConversionRank.StringToPrimitive:
                        return System.Convert.ChangeType(value, desiredType);

                    case ConversionRank.StringToEnum:
                    case ConversionRank.EnumUnderlying:
                        return Enum.Parse(desiredType, value.ToString(), false);

                    case ConversionRank.None:
                    default:
                        throw new NotImplementedException($"Don't know how to handle {nameof(ConversionRank)} '{rank.Value}'.");
                }
            }

            #endregion

            private Exception GetRemotableException(Exception ex)
            {
                if (ex is TargetInvocationException e)
                    ex = ex.InnerException;

                if (Serialization.IsSerializable(ex))
                    return ex;

                return new RemoteException(ex.Message, ex.GetType());
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
