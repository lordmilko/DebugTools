using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using DebugTools.SOS;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "SOSThread")]
    public class GetSOSThread : SOSCmdlet
    {
        [Alias("ThreadId")]
        [Parameter(Position = 0)]
        public int Id { get; set; }

        protected override void ProcessRecordEx()
        {
            IEnumerable<SOSThreadInfo> threads = HostApp.GetSOSThreads(Process);

            if (MyInvocation.BoundParameters.ContainsKey(nameof(Id)))
                threads = threads.Where(t => t.ThreadId == Id);

            foreach (var thread in threads)
                WriteObject(thread);
        }
    }
}
