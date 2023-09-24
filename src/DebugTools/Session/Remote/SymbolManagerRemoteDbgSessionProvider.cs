using System.Diagnostics;

namespace DebugTools
{
    class SymbolManagerRemoteDbgSessionProvider : RemoteDbgSessionProvider<SymbolManager>
    {
        public override DbgSessionType SessionType => DbgSessionType.SymbolManager;

        protected override SymbolManager CreateSubSessionInternal(Process process) =>
            new SymbolManager(process);
    }
}
