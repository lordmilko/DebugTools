using System.Management.Automation;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommunications.Disconnect, "SOSProcess")]
    public class DisconnectSOSProcess : DebugToolsCmdlet
    {
        [Parameter(Mandatory = false, ValueFromPipeline = true)]
        public LocalSOSProcess Process { get; set; }

        protected override void ProcessRecord()
        {
            var process = Process;

            if (process == null)
                process = DebugToolsSessionState.Services.GetImplicitService<LocalSOSProcess>(false);

            if (process != null)
            {
                DebugToolsSessionState.Services.Close(process.Process.Id, process);
            }
        }
    }
}
