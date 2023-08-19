using System.Diagnostics;
using DebugTools.Host;

namespace DebugTools
{
    class UiMessageRemoteDbgSessionProvider : RemoteDbgSessionProvider<UiMessageSession>
    {
        public override DbgServiceType ServiceType => DbgServiceType.Ui;

        protected override UiMessageSession CreateServiceInternal(Process process) =>
            new UiMessageSession(process);
    }
}
