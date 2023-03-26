using System;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommunications.Connect, "SOSProcess")]
    public class ConnectSOSProcess : DebugToolsCmdlet
    {
        [Parameter(Mandatory = false, ValueFromPipeline = true, ParameterSetName = ParameterSet.Default)]
        public Process Process { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Manual, Position = 0)]
        public int ProcessId { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Dbg { get; set; }

        protected override void ProcessRecord()
        {
            var process = GetProcess();

            var existing = DebugToolsSessionState.SOSProcesses.FirstOrDefault(p => p.Process.Id == process.Id);

            if (existing != null)
            {
                WriteWarning($"Cannot connect to process {process.Id}: process is already connected.");

                WriteObject(existing);
            }
            else
            {
                var hostApp = DebugToolsSessionState.GetDetectedHost(process, Dbg);

                var handle = hostApp.CreateSOSProcess(process.Id, false);

                var sosProcess = new LocalSOSProcess(handle);

                DebugToolsSessionState.SOSProcesses.Add(sosProcess);

                WriteObject(sosProcess);
            }
        }

        private Process GetProcess()
        {
            switch (ParameterSetName)
            {
                case ParameterSet.Default:
                    return Process;

                case ParameterSet.Manual:
                    return Process.GetProcessById(ProcessId);

                default:
                    throw new UnknownParameterSetException(ParameterSetName);
            }
        }
    }
}
