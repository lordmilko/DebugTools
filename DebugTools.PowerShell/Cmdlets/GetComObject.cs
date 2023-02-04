using System.Diagnostics;
using System.Management.Automation;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "ComObject")]
    public class GetComObject : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        public int ProcessId { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Dbg { get; set; }

        protected override void ProcessRecord()
        {
            var process = Process.GetProcessById(ProcessId);

            var hostApp = DebugToolsSessionState.GetDetectedHost(process, Dbg);

            var results = hostApp.GetComObjects(ProcessId);

            foreach (var result in results)
                WriteObject(result);
        }
    }
}
