using System;
using System.Diagnostics;
using System.Management.Automation;
using DebugTools.Host;
using DebugTools.Profiler;

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
                Process = DebugToolsSessionState.Services.GetImplicitOrFallbackProcess();
            }

            HostApp = DebugToolsSessionState.Services.GetDetectedHost(Process, Dbg);

            ProcessRecordEx();
        }

        protected abstract void ProcessRecordEx();
    }
}
