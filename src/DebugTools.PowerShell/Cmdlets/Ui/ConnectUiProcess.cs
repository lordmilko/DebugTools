using System;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using DebugTools.Ui;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommunications.Connect, "UiProcess")]
    public class ConnectUiProcess : DebugToolsCmdlet
    {
        [Parameter(Mandatory = false, ValueFromPipeline = true, ParameterSetName = ParameterSet.Default)]
        public Process Process { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Manual, Position = 0)]
        public Either<string, int> ProcessNameOrId { get; set; }

        protected override void ProcessRecord()
        {
            var process = GetProcess();

            var existing = DebugToolsSessionState.UiSessions.FirstOrDefault(p => p.Process.Id == process.Id);

            if (existing != null)
            {
                WriteWarning($"Cannot connect to process {process.Id}: process is already connected.");

                WriteObject(existing);
            }
            else
            {
                var session = new UiSession(process);

                DebugToolsSessionState.UiSessions.Add(session);

                WriteObject(session);
            }
        }

        private Process GetProcess()
        {
            switch (ParameterSetName)
            {
                case ParameterSet.Default:
                    return Process;

                case ParameterSet.Manual:
                    if (ProcessNameOrId.IsLeft)
                    {
                        var candidates = Process.GetProcessesByName(ProcessNameOrId.Left);

                        if (candidates.Length == 1)
                            return candidates[0];

                        if (candidates.Length == 0)
                            throw new InvalidOperationException($"Could not find any processes with name '{ProcessNameOrId.Left}'");

                        if (candidates.Length > 1)
                            throw new InvalidOperationException($"Found more than one process with name '{ProcessNameOrId.Left}'. Please specify a specific Process ID or Process object.");
                    }

                    return Process.GetProcessById(ProcessNameOrId.Right);

                default:
                    throw new UnknownParameterSetException(ParameterSetName);
            }
        }
    }
}
