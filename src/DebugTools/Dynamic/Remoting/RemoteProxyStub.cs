using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using DebugTools.Host;

namespace DebugTools.Dynamic
{
    public class RemoteProxyStub : MarshalByRefObject
    {
        private object remoteValue;
        private IRemotingMarshaller marshaller;
        private ReflectionProxy reflectionProxy;

        //We can't use the Type object itself, since the type might be in an assembly that doesn't exist on local end!
        public string Type => remoteValue.GetType().Name;

        public RemoteProxyStubKind Kind { get; }

        private ReflectionProxy.ReflectionMetaProxy MetaProxy => ReflectionProxy.ReflectionMetaProxy.Instance;

        private static ConditionalWeakTable<object, RemoteProxyStub> cache = new ConditionalWeakTable<object, RemoteProxyStub>();

        public static object Wrap(object remoteValue, IRemotingMarshaller marshaller) => WrapOutput(remoteValue, marshaller);

        public static RemoteProxyStub New(object remoteValue, IRemotingMarshaller marshaller)
        {
            if (cache.TryGetValue(remoteValue, out var existing))
                return existing;

            RemoteProxyStubKind kind;

            if (remoteValue is IDictionary)
                kind = RemoteProxyStubKind.Dictionary;
            else if (remoteValue is IEnumerable) //string would not be wrapped thus its safe to just check IEnumerable
                kind = RemoteProxyStubKind.Enumerable;
            else if (remoteValue is IEnumerator)
                kind = RemoteProxyStubKind.Enumerator;
            else
                kind = RemoteProxyStubKind.Normal;

            var result = new RemoteProxyStub(remoteValue, kind, marshaller);
            result = marshaller.Marshal(result);

            cache.Add(remoteValue, result);
            return result;
        }

        protected RemoteProxyStub(object remoteValue, RemoteProxyStubKind kind, IRemotingMarshaller marshaller)
        {
            if (remoteValue == null)
                throw new ArgumentNullException(nameof(remoteValue));

            if (marshaller == null)
                throw new ArgumentNullException(nameof(marshaller));

            this.remoteValue = remoteValue;
            this.marshaller = marshaller;
            reflectionProxy = new ReflectionProxy(remoteValue);

            Kind = kind;
        }

        #region Index

        public bool TryGetIndex(object[] indexes, out object value)
        {
            indexes = UnwrapInput(indexes);

            if (MetaProxy.TryGetIndex(reflectionProxy, null, indexes, out var rawValue))
            {
                value = WrapOutput(rawValue, marshaller);
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
                value = WrapOutput(rawValue, marshaller);
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
                value = WrapOutput(rawValue, marshaller);
                return true;
            }

            value = null;
            return false;
        }

        #endregion

        private static object WrapOutput(object value, IRemotingMarshaller marshaller)
        {
            if (value == null)
                return null;

            if (value is RemoteProxyStub)
                return value;

            //MemberInfo covers members and types
            if (!(value is MemberInfo) && !(value is Module) && !(value is Assembly) && !(value is AppDomain) && Serialization.IsSerializable(value))
            {
                var desiredDir = Path.GetDirectoryName(typeof(string).Assembly.Location);
                var candidateDir = Path.GetDirectoryName(value.GetType().Assembly.Location);

                //Just because the type is serializable doesn't mean that the type exists in the local process that the value is going to be returned into.
                //Note that gotcha types like SZArrayEnumerator shouldn't be returned as is; they're actually enumerators and so need to be wrapped
                if (desiredDir.Equals(candidateDir, StringComparison.OrdinalIgnoreCase) && !value.GetType().Name.Contains("Enumerator"))
                {
                    var type = value.GetType();

                    if (type.IsArray)
                    {
                        //It's a simple array, but is its element type safe?

                        //Just in case we have a nested array, ensure to unwrap fully
                        while (type.IsArray)
                            type = type.GetElementType();

                        var elementTypeDir = Path.GetDirectoryName(type.Assembly.Location);

                        if (!desiredDir.Equals(elementTypeDir, StringComparison.OrdinalIgnoreCase))
                            return New(value, marshaller);
                    }

                    return value;
                }
            }

            return New(value, marshaller);
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
