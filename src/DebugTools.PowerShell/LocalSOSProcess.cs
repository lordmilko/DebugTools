using System.Diagnostics;
using DebugTools.SOS;

namespace DebugTools.PowerShell
{
    public class LocalSOSProcess
    {
        public Process Process { get; }

        private SOSProcessHandle handle;

        internal LocalSOSProcess(SOSProcessHandle handle)
        {
            this.handle = handle;

            Process = Process.GetProcessById(handle.ProcessId);
        }

        public static implicit operator SOSProcessHandle(LocalSOSProcess process)
        {
            return process.handle;
        }
    }
}
