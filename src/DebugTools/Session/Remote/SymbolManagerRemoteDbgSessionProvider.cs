using System.Diagnostics;

namespace DebugTools
{
    class SymbolManagerRemoteDbgSessionProvider : RemoteDbgSessionProvider<SymbolManager>
    {
        public override DbgServiceType ServiceType => DbgServiceType.SymbolManager;

        protected override SymbolManager CreateServiceInternal(Process process) =>
            new SymbolManager(process);
    }
}
