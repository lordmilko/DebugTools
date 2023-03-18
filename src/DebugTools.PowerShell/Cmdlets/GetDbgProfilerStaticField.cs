using System.Management.Automation;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "DbgProfilerStaticField")]
    public class GetDbgProfilerStaticField : ProfilerSessionCmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        public string Name { get; set; }

        [Parameter(Mandatory = false)]
        public int ThreadId { get; set; }

        [Parameter(Mandatory = false)]
        public int ValueDepth { get; set; }
        protected override void ProcessRecordEx()
        {
            var result = Session.GetStaticField(Name, ThreadId, ValueDepth);

            WriteObject(result);
        }
    }
}
