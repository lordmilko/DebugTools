using System.Management.Automation;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Clear, "SOSDacCache")]
    public class ClearSOSDacCache : SOSCmdlet
    {
        protected override void ProcessRecordEx()
        {
            HostApp.Flush(Process);
        }
    }
}
