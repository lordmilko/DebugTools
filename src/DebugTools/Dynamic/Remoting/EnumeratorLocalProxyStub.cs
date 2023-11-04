using System.Collections;

namespace DebugTools.Dynamic
{
    class EnumeratorLocalProxyStub : LocalProxyStub, IEnumerator
    {
        private dynamic remoteEnumerator;

        public EnumeratorLocalProxyStub(RemoteProxyStub remoteStub, ILocalProxyNotifier notifier) : base(remoteStub, null)
        {
            remoteEnumerator = remoteStub;
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

        object IEnumerator.Current => Current;
    }
}
