using System.Management.Automation;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommunications.Disconnect, "SOSProcess")]
    public class DisconnectSOSProcess : SOSCmdlet
    {
        protected override void ProcessRecordEx()
        {
            DebugToolsSessionState.SOSProcesses.Remove(Process);
            HostApp.RemoveSOSProcess(Process);
        }
    }
}
