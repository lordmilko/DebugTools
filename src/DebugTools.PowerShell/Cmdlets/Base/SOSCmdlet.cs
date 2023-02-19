using System.Management.Automation;
using DebugTools.Host;

namespace DebugTools.PowerShell.Cmdlets
{
    public abstract class SOSCmdlet : PSCmdlet
    {
        [Parameter(Mandatory = false, ValueFromPipeline = true)]
        public LocalSOSProcess Process { get; set; }

        protected HostApp HostApp { get; private set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Dbg { get; set; }

        protected sealed override void ProcessRecord()
        {
            if (Process == null)
            {
                if (DebugToolsSessionState.SOSProcesses.Count == 0 && DebugToolsSessionState.ProfilerSessions.Count > 0)
                {
                    var process = DebugToolsSessionState.GetImplicitProfilerSession().Process;

                    HostApp = DebugToolsSessionState.GetDetectedHost(process, Dbg);

                    Process = new LocalSOSProcess(HostApp.CreateSOSProcess(process.Id));
                }
                else
                {
                    Process = DebugToolsSessionState.GetImplicitSOSProcess();

                    HostApp = DebugToolsSessionState.GetDetectedHost(Process.Process, Dbg);
                }
            }
            else
            {
                HostApp = DebugToolsSessionState.GetDetectedHost(Process.Process, Dbg);
            }

            ProcessRecordEx();
        }

        protected abstract void ProcessRecordEx();
    }
}
