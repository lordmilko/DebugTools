using System;

namespace DebugTools.Dynamic
{
    /// <summary>
    /// Represents a type that should receive notifications of newly constructed <see cref="LocalProxyStub"/> objects.
    /// </summary>
    public interface ILocalProxyNotifier
    {
        void Notify(LocalProxyStub localProxyStub);

        IDisposable DisableEnumeration();
    }
}
