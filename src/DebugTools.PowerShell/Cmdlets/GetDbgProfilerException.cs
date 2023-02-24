using System.Linq;
using System.Management.Automation;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "DbgProfilerException")]
    public class GetDbgProfilerException : ProfilerSessionCmdlet
    {
        [Parameter(Mandatory = false, Position = 0)]
        public string[] Type { get; set; }

        public int[] ThreadId { get; set; }

        protected override void ProcessRecordEx()
        {
            var result = Session.LastTrace;

            if (result != null)
            {
                var exceptions = result.SelectMany(r => r.Exceptions.Values);

                if (Type != null)
                    exceptions = FilterByWildcardArray(Type, exceptions, e => e.Type);

                if (ThreadId != null)
                    exceptions = exceptions.Where(e => ThreadId.Contains(e.ThreadId));

                foreach (var exception in exceptions)
                    WriteObject(exception);
            }
        }
    }
}
