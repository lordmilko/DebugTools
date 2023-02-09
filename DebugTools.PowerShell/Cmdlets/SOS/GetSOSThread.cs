using System.Collections.Generic;
using System.Management.Automation;
using DebugTools.SOS;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "SOSThread")]
    public class GetSOSThread : SOSCmdlet
    {
        protected override void ProcessRecordEx()
        {
            var threads = HostApp.GetSOSThreads(Process);

            foreach (var thread in threads)
                WriteObject(thread);
        }
    }
}
