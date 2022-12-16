using System.Management.Automation;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Stop, "DbgProfiler")]
    public class StopDbgProfiler : ProfilerSessionCmdlet
    {
        protected override void ProcessRecordEx()
        {
            Session.Dispose();
        }
    }
}