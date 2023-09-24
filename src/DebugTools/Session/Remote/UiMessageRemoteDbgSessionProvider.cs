using System.Diagnostics;
using DebugTools.Ui;

namespace DebugTools
{
    class UiMessageRemoteDbgSessionProvider : RemoteDbgSessionProvider<UiMessageSession>
    {
        public override DbgSessionType SessionType => DbgSessionType.Ui;

        protected override UiMessageSession CreateSubSessionInternal(Process process) =>
            new UiMessageSession(process);
    }
}
