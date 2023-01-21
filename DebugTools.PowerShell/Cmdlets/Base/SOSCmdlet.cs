using System.Management.Automation;
using ClrDebug;
using DebugTools.SOS;

namespace DebugTools.PowerShell.Cmdlets
{
    public abstract class SOSCmdlet : PSCmdlet
    {
        [Parameter(Mandatory = false, ValueFromPipeline = true)]
        public SOSProcess Process { get; set; }

        public SOSDacInterface SOS => Process.SOS;

        protected sealed override void ProcessRecord()
        {
            if (Process == null)
            {
                if (DebugToolsSessionState.SOSProcesses.Count == 0 && DebugToolsSessionState.ProfilerSessions.Count > 0)
                    Process = new SOSProcess(DebugToolsSessionState.GetImplicitProfilerSession().Process);
                else
                    Process = DebugToolsSessionState.GetImplicitSOSProcess();
            }

            ProcessRecordEx();
        }

        protected abstract void ProcessRecordEx();
    }
}
