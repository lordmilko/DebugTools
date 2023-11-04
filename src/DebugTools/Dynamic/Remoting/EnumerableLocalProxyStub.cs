using System.Collections;
using System.Collections.Generic;

namespace DebugTools.Dynamic
{
    class EnumerableLocalProxyStub : LocalProxyStub, IEnumerable<object>
    {
        public EnumerableLocalProxyStub(RemoteProxyStub remoteStub, ILocalProxyNotifier notifier) : base(remoteStub, notifier)
        {
        }

        public IEnumerator<object> GetEnumerator() => new LocalProxyStubEnumerator(this, notifier);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private class LocalProxyStubEnumerator : IEnumerator<object>
        {
            private dynamic remoteEnumerator;

            public LocalProxyStubEnumerator(dynamic localStub, ILocalProxyNotifier notifier)
            {
                /* When a method yields its results, the compiler generates a state machine that implements both IEnumerable and IEnumerator
                 * that is returned from the method. When GetEnumerator is called on a different thread than the thread the state machine
                 * was created on, a new state machine will be created and returned. This creates a big problem:
                 * 
                 *     - Because we're now returning a new object, our cache won't see it as being the same state machine we retrieved previously
                 *     - Which means that a new EnumerableLocalProxyStub will be created
                 *     - Which means ILocalProxyNotifier.Notify() (and therefore EnsurePSObject()) will be called again
                 *     - But because the enumerator implements IEnumerable as well, it gets proxied as an EnumerableLocalProxyStub
                 *     - Which means that EnsurePSObject() will try and enumerate the enumerator in order to call EnsurePSObject() on all elements of the collection
                 *     
                 * Thus, we get an infinite loop wherein EnsurePSObject() keeps thinking the IEnumerator is a new IEnumerable object that needs to be wrapped.
                 * To prevent this, we notify EnsurePSObject() that while we're retrieving the IEnumerator it should not attempt to process any IEnumerable
                 * proxies that it sees. Note that you can't just not call GetEnumerator() in the case of a state machine; the state will be -2 by default,
                 * and calling MoveNext() won't do anything until GetEnumerator() has been called and the state has been set to 0. */
                using (notifier.DisableEnumeration())
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

            public void Dispose()
            {
            }
        }
    }
}
