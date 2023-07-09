using System.Collections;

namespace DebugTools.Dynamic
{
    public class EnumerableRemoteProxyStub : RemoteProxyStub
    {
        private IEnumerable remoteValue;

        internal EnumerableRemoteProxyStub(IEnumerable remoteValue) : base(remoteValue)
        {
            this.remoteValue = remoteValue;
        }
    }
}
