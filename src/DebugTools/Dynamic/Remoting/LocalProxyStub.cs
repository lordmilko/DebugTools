using System;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting;

namespace DebugTools.Dynamic
{
    [DebuggerDisplay("[Proxy] {type,nq}")]
    public partial class LocalProxyStub : IDynamicMetaObjectProvider
    {
#if DEBUG
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string type;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        private string[] members;
#endif

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private RemoteProxyStub remoteStub { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        protected ILocalProxyNotifier notifier;

        private static ConditionalWeakTable<string, LocalProxyStub> cache = new ConditionalWeakTable<string, LocalProxyStub>();

        internal static object Wrap(object value, ILocalProxyNotifier notifier = null)
        {
            if (value is RemoteProxyStub r)
                return New(r, notifier);

            return value;
        }

        internal static LocalProxyStub New(RemoteProxyStub remoteStub, ILocalProxyNotifier notifier = null)
        {
            var objRef = RemotingServices.GetObjRefForProxy(remoteStub);

            //Caching on the MarshalByRefObject or ObjRef doesn't always work, so we use the object URI instead
            if (cache.TryGetValue(objRef.URI, out var existing))
                return existing;

            LocalProxyStub result;

            //Checks via "is" and GetType() can sometimes be wrong when we query our remoted object
            switch (remoteStub.Kind)
            {
                case RemoteProxyStubKind.Normal:
                    result = new LocalProxyStub(remoteStub, notifier);
                    break;

                case RemoteProxyStubKind.Enumerator:
                    result = new EnumeratorLocalProxyStub(remoteStub, notifier);
                    break;

                case RemoteProxyStubKind.Enumerable:
                case RemoteProxyStubKind.Dictionary:
                    result = new EnumerableLocalProxyStub(remoteStub, notifier);
                    break;

                default:
                    throw new NotImplementedException($"Don't know how to handle {nameof(RemoteProxyStubKind)} '{remoteStub.Kind}'.");
            }

            cache.Add(objRef.URI, result);
            return result;
        }

        protected LocalProxyStub(RemoteProxyStub remoteStub, ILocalProxyNotifier notifier)
        {
            this.remoteStub = remoteStub;
            this.notifier = notifier;

#if DEBUG
            type = remoteStub.Type;
            members = GetMetaObject(Expression.Parameter(typeof(object))).GetDynamicMemberNames().ToArray();
#endif

            notifier?.Notify(this);
        }

        public DynamicMetaObject GetMetaObject(Expression parameter) =>
            new DynamicMetaObject<LocalProxyStub>(parameter, this, new LocalMetaProxy());

        public override string ToString() => ((dynamic) this).ToString();

        public override bool Equals(object obj)
        {
            if (!(obj is LocalProxyStub))
                return false;

            return ((dynamic)this).Equals(obj);
        }

        public override int GetHashCode() => ((dynamic) this).GetHashCode();
    }
}
