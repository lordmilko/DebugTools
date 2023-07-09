using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.CSharp.RuntimeBinder;

namespace DebugTools.Dynamic
{
    partial class LocalProxyStub
    {
        class LocalMetaProxy : DynamicMetaProxy<LocalProxyStub>, IFallbackResultProvider
        {
            public override bool TryGetIndex(LocalProxyStub instance, GetIndexBinder binder, object[] indexes, out object value)
            {
                indexes = UnwrapInput(indexes);

                var result = instance.remoteStub.TryGetIndex(indexes, out var remoteValue);

                if (result)
                {
                    value = WrapOutput(remoteValue);
                    return true;
                }

                value = null;
                return false;
            }

            public override bool TrySetIndex(LocalProxyStub instance, SetIndexBinder binder, object[] indexes, object value) =>
                instance.remoteStub.TrySetIndex(UnwrapInput(indexes), UnwrapInput(value));

            #region Member

            public override bool TryGetMember(LocalProxyStub instance, GetMemberBinder binder, out object value)
            {
                var result = instance.remoteStub.TryGetMember(binder.Name, binder.IgnoreCase, out var remoteValue);

                if (result)
                {
                    value = WrapOutput(remoteValue);
                    return true;
                }

                value = null;
                return false;
            }

            public override bool TrySetMember(LocalProxyStub instance, SetMemberBinder binder, object value) =>
                instance.remoteStub.TrySetMember(binder.Name, binder.IgnoreCase, UnwrapInput(value));

            public override bool TryInvokeMember(LocalProxyStub instance, InvokeMemberBinder binder, object[] args, out object value)
            {
                args = UnwrapInput(args);

                var result = instance.remoteStub.TryInvokeMember(binder.Name, binder.IgnoreCase, args, out var remoteValue);

                if (result)
                {
                    value = WrapOutput(remoteValue);
                    return true;
                }

                value = null;
                return false;
            }

            #endregion

            private object UnwrapInput(object value)
            {
                if (value is LocalProxyStub l)
                    return l.remoteStub;

                return value;
            }

            private object[] UnwrapInput(object[] values)
            {
                if (values == null)
                    return values;

                var unwrapped = new object[values.Length];

                for (var i = 0; i < values.Length; i++)
                    unwrapped[i] = UnwrapInput(values[i]);

                return unwrapped;
            }

            private object WrapOutput(object value)
            {
                if (value is RemoteProxyStub r)
                    return New(r);

                return value;
            }

            public override IEnumerable<string> GetDynamicMemberNames(LocalProxyStub instance) =>
                instance.remoteStub.GetDynamicMemberNames();

            public DynamicMetaObject GetFallbackResult(
                DynamicMetaObjectBinder binder,
                Expression instance,
                Expression[] args,
                BindingRestrictions restrictions)
            {
                return new DynamicMetaObject(
                    Expression.Throw(
                        Expression.Call(
                            typeof(LocalMetaProxy).GetMethod(nameof(GetBinderException)),
                            new[] {Expression.Constant(binder), instance, Expression.Constant(args.Select(a => a.Type).ToArray())}
                        ),
                        binder.ReturnType
                    ),
                    restrictions
                );

                throw new System.NotImplementedException();
            }

            public static RuntimeBinderException GetBinderException(
                DynamicMetaObjectBinder binder,
                LocalProxyStub instance,
                Type[] argTypes)
            {
                var missingMember = "'{0}' does not contain a definition for '{1}'";
                var noIndex = "Cannot apply indexing with [{0}] to an expression of type '{1}'";

                var argNames = string.Join(", ", argTypes.Select(t => t.ToString()));

                var type = instance.remoteStub.Type;

                string message;

                if (binder is GetIndexBinder gi)
                    message = string.Format(noIndex, argNames, type);
                else if (binder is SetIndexBinder si)
                    message = string.Format(noIndex, argNames, type);
                else if (binder is GetMemberBinder gm)
                    message = string.Format(missingMember, type, gm.Name);
                else if (binder is SetMemberBinder sm)
                    message = string.Format(missingMember, type, sm.Name);
                else if (binder is InvokeMemberBinder im)
                    message = string.Format(missingMember, type, im.Name);
                else
                    throw new NotImplementedException($"Don't know how to generate fallback for binder of type '{binder.GetType().Name}'");

                return new RuntimeBinderException(message);
            }
        }
    }
}
