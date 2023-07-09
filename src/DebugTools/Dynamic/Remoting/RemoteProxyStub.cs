using System;
using System.Collections;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DebugTools.Dynamic
{
    public class RemoteProxyStub : MarshalByRefObject
    {
        private object remoteValue;
        private ReflectionProxy reflectionProxy;

        public Type Type => remoteValue.GetType();

        private ReflectionProxy.ReflectionMetaProxy MetaProxy => ReflectionProxy.ReflectionMetaProxy.Instance;

        private static ConditionalWeakTable<object, RemoteProxyStub> cache = new ConditionalWeakTable<object, RemoteProxyStub>();

        public static RemoteProxyStub New(object remoteValue)
        {
            if (cache.TryGetValue(remoteValue, out var existing))
                return existing;

            RemoteProxyStub result;

            if (remoteValue is IEnumerable e)
                result =new EnumerableRemoteProxyStub(e);
            else
                result = new RemoteProxyStub(remoteValue);

            cache.Add(remoteValue, result);
            return result;
        }

        protected RemoteProxyStub(object remoteValue)
        {
            if (remoteValue == null)
                throw new ArgumentNullException(nameof(remoteValue));

            this.remoteValue = remoteValue;
            this.reflectionProxy = new ReflectionProxy(remoteValue);
        }

        #region Index

        public bool TryGetIndex(object[] indexes, out object value)
        {
            indexes = UnwrapInput(indexes);

            if (MetaProxy.TryGetIndex(reflectionProxy, null, indexes, out var rawValue))
            {
                value = WrapOutput(rawValue);
                return true;
            }

            value = null;
            return false;
        }

        public bool TrySetIndex(object[] indexes, object value) =>
            MetaProxy.TrySetIndex(reflectionProxy, null, UnwrapInput(indexes), UnwrapInput(value));

        #endregion
        #region Member

        public bool TryGetMember(string name, bool ignoreCase, out object value)
        {
            if (MetaProxy.TryGetMember(reflectionProxy, new FakeGetMemberBinder(name, ignoreCase), out var rawValue))
            {
                value = WrapOutput(rawValue);
                return true;
            }

            value = null;
            return false;
        }

        public bool TrySetMember(string name, bool ignoreCase, object value) =>
            MetaProxy.TrySetMember(reflectionProxy, new FakeSetMemberBinder(name, ignoreCase), UnwrapInput(value));

        public bool TryInvokeMember(string name, bool ignoreCase, object[] args, out object value)
        {
            args = UnwrapInput(args);

            //No need to worry about passing along binder.IgnoreCase. Our fake binder is only used to access the name in our Try* overrides anyway!

            if (MetaProxy.TryInvokeMember(reflectionProxy, new FakeInvokeMemberBinder(name, ignoreCase), args, out var rawValue))
            {
                value = WrapOutput(rawValue);
                return true;
            }

            value = null;
            return false;
        }

        #endregion

        private object WrapOutput(object value)
        {
            if (value == null)
                return null;

            if (value is MarshalByRefObject)
                return value;

            if (Serialization.IsSerializable(value))
                return value;

            return New(value);
        }

        private object UnwrapInput(object value)
        {
            if (value == null)
                return null;

            if (value is RemoteProxyStub r)
                return r.remoteValue;

            return value;
        }

        private object[] UnwrapInput(object[] values)
        {
            if (values == null)
                return null;

            var unmarshalled = new object[values.Length];

            for (var i = 0; i < values.Length; i++)
                unmarshalled[i] = UnwrapInput(values[i]);

            return unmarshalled;
        }

        public string[] GetDynamicMemberNames() => MetaProxy.GetDynamicMemberNames(reflectionProxy).ToArray();

        public override object InitializeLifetimeService() => null;
    }
}
