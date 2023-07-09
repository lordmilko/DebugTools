using System.Collections;

namespace DebugTools.Dynamic
{
    partial class EnumerableLocalProxyStub : LocalProxyStub, IEnumerable
    {
        public EnumerableLocalProxyStub(EnumerableRemoteProxyStub remoteStub) : base(remoteStub)
        {
        }

        public IEnumerator GetEnumerator() => new LocalProxyStubEnumerator(this);

        private class LocalProxyStubEnumerator : IEnumerator
        {
            private dynamic remoteEnumerator;

            public LocalProxyStubEnumerator(dynamic localStub)
            {
                remoteEnumerator = localStub.GetEnumerator();
            }

            public object Current { get; private set; }

            public bool MoveNext()
            {
                var result = remoteEnumerator.MoveNext();

                if (result)
                    Current = remoteEnumerator.Current;

                return result;
            }

            public void Reset() => remoteEnumerator.Reset();
        }
    }
}
