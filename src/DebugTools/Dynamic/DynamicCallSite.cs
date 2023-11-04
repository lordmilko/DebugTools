using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CSharp.RuntimeBinder;
using Binder = Microsoft.CSharp.RuntimeBinder.Binder;

namespace DebugTools.Dynamic
{
    static class DynamicCallSite
    {
        #region Void

        public static void CallVoid(this object instance, string name) =>
            CallInternal<Action<CallSite, object>>(instance, name);

        public static void CallAnyVoid(this object instance, string name, Type[] parameterTypes, object[] arguments) =>
            CallAnyInternal(instance, name, parameterTypes, arguments, GetActionType);

        public static void CallVoid<T0>(this object instance, string name, T0 arg0) =>
            CallInternal<Action<CallSite, object, T0>>(instance, name, arg0);

        #endregion
        #region Result

        public static TResult CallResult<TResult>(this object instance, string name) =>
            (TResult) CallInternal<Func<CallSite, object, TResult>>(instance, name);

        public static object CallAnyResult(this object instance, string name, Type[] parameterTypes, object[] arguments) =>
            CallAnyInternal(instance, name, parameterTypes.Concat(new[] { typeof(object) }).ToArray(), arguments, GetFuncType);

        public static TResult CallResult<T0, TResult>(this object instance, string name, T0 arg0) =>
            (TResult) CallInternal<Func<CallSite, object, T0, TResult>>(instance, name, arg0);

        #endregion

        private static object CallInternal<TAction>(
            object instance,
            string name,
            params object[] args) where TAction : Delegate
        {
            var parameters = typeof(TAction).GetMethod("Invoke").GetParameters();

            var arguments = new List<CSharpArgumentInfo>
            {
                CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
            };

            for (var i = 2; i < parameters.Length; i++)
                arguments.Add(CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null));

            var callSite = CallSite<TAction>.Create(
                Binder.InvokeMember(
                    flags: typeof(TAction).Name.Contains("Action") ? CSharpBinderFlags.ResultDiscarded : CSharpBinderFlags.None,
                    name: name,
                    typeArguments: null,
                    context: instance.GetType(),
                    argumentInfo: arguments
                )
            );

            var internalArgs = new List<object>
            {
                callSite,
                instance
            };
            internalArgs.AddRange(args);

            return callSite.Target.DynamicInvoke(internalArgs.ToArray());
        }

        private static object CallAnyInternal(
            this object instance,
            string name,
            Type[] parameterTypes,
            object[] arguments,
            Func<int, Type> getDelegateType)
        {
            var typeArguments = new List<Type>
            {
                typeof(CallSite),
                typeof(object)
            };
            typeArguments.AddRange(parameterTypes);

            var delegateType = getDelegateType(typeArguments.Count).MakeGenericType(typeArguments.ToArray());

            var callInternalDef = typeof(DynamicCallSite).GetMethod("CallInternal", BindingFlags.Static | BindingFlags.NonPublic);

            var callInternal = callInternalDef.MakeGenericMethod(delegateType);

            return callInternal.Invoke(null, new[] { instance, name, arguments });
        }

        private static Type GetActionType(int numArgs)
        {
            switch (numArgs)
            {
                case 0:
                    return typeof(Action);

                case 1:
                    return typeof(Action<>);

                case 2:
                    return typeof(Action<,>);

                case 3:
                    return typeof(Action<,,>);

                default:
                    throw new NotImplementedException($"Don't know how to handle action with {numArgs} args.");
            }
        }

        private static Type GetFuncType(int numArgs)
        {
            switch (numArgs)
            {
                case 1:
                    return typeof(Func<>);

                case 2:
                    return typeof(Func<,>);

                case 3:
                    return typeof(Func<,,>);

                case 4:
                    return typeof(Func<,,,>);

                default:
                    throw new NotImplementedException($"Don't know how to handle func with {numArgs} args.");
            }
        }
    }
}
