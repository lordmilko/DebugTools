using System.Diagnostics;
using System.Dynamic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting;

namespace DebugTools.Dynamic
{
    partial class LocalProxyStub : IDynamicMetaObjectProvider
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private RemoteProxyStub remoteStub { get; }

        private static ConditionalWeakTable<string, LocalProxyStub> cache = new ConditionalWeakTable<string, LocalProxyStub>();

        public static LocalProxyStub New(RemoteProxyStub remoteStub)
        {
            var objRef = RemotingServices.GetObjRefForProxy(remoteStub);

            //Caching on the MarshalByRefObject or ObjRef doesn't always work, so we use the object URI instead
            if (cache.TryGetValue(objRef.URI, out var existing))
                return existing;

            LocalProxyStub result;

            if (remoteStub is EnumerableRemoteProxyStub e)
                result = new EnumerableLocalProxyStub(e);
            else
                result = new LocalProxyStub(remoteStub);

            cache.Add(objRef.URI, result);
            return result;
        }

        protected LocalProxyStub(RemoteProxyStub remoteStub)
        {
            this.remoteStub = remoteStub;
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
