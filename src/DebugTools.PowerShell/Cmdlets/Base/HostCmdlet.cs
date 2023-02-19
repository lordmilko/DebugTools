using System.Diagnostics;
using System.Management.Automation;
using DebugTools.Host;

namespace DebugTools.PowerShell.Cmdlets
{
    public abstract class HostCmdlet : PSCmdlet
    {
        public Process Process { get; set; }

        [Parameter(Mandatory = false)]
        public int ProcessId { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Dbg { get; set; }

        protected HostApp HostApp { get; private set; }

        protected sealed override void ProcessRecord()
        {
            if (MyInvocation.BoundParameters.ContainsKey(nameof(ProcessId)))
                Process = Process.GetProcessById(ProcessId);
            else
            {
                if (DebugToolsSessionState.SOSProcesses.Count == 0 && DebugToolsSessionState.ProfilerSessions.Count > 0)
                    Process = DebugToolsSessionState.GetImplicitProfilerSession().Process;
                else
                    Process = DebugToolsSessionState.GetImplicitSOSProcess().Process;
            }

            HostApp = DebugToolsSessionState.GetDetectedHost(Process, Dbg);

            ProcessRecordEx();
        }

        protected abstract void ProcessRecordEx();
    }
}
