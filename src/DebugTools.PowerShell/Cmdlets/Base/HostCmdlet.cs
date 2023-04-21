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
                if (DebugToolsSessionState.SOSProcesses.Count == 0 && DebugToolsSessionState.ProfilerSessions.Count > 0)
                {
                    var session = DebugToolsSessionState.GetImplicitProfilerSession();

                    if (session.Type == ProfilerSessionType.XmlFile)
                        throw new InvalidOperationException($"Cannot execute cmdlet: no -Session was specified and no global Session could be found in the PowerShell session.");

                    Process = ((LiveProfilerTarget)session.Target).Process;
                }
                else
                    Process = DebugToolsSessionState.GetImplicitSOSProcess().Process;
            }

            HostApp = DebugToolsSessionState.GetDetectedHost(Process, Dbg);

            ProcessRecordEx();
        }

        protected abstract void ProcessRecordEx();
    }
}
