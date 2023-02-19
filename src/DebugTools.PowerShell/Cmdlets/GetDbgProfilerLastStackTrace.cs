using System.Management.Automation;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "DbgProfilerLastStackTrace")]
    public class GetDbgProfilerLastStackTrace : ProfilerSessionCmdlet
    {
        protected override void ProcessRecordEx()
        {
            var result = Session.LastTrace;

            if (result != null)
            {
                foreach (var item in result)
                    WriteObject(item.Root);
            }
        }
    }
}