using System.Diagnostics;
using DebugTools.SOS;

namespace DebugTools
{
    class SOSRemoteDbgSessionProvider : RemoteDbgSessionProvider<SOSProcess>
    {
        public override DbgSessionType SessionType => DbgSessionType.SOS;

        protected override SOSProcess CreateSubSessionInternal(Process process) =>
            new SOSProcess(process);
    }
}
