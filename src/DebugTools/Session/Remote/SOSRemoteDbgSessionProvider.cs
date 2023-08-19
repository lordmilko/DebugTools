using System.Diagnostics;
using DebugTools.SOS;

namespace DebugTools
{
    class SOSRemoteDbgSessionProvider : RemoteDbgSessionProvider<SOSProcess>
    {
        public override DbgServiceType ServiceType => DbgServiceType.SOS;

        protected override SOSProcess CreateServiceInternal(Process process) =>
            new SOSProcess(process);
    }
}
