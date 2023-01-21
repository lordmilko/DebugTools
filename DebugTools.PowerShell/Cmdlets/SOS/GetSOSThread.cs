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
            var threadStore = SOS.ThreadStoreData;

            var currentThread = threadStore.firstThread;

            var threads = new List<SOSThreadInfo>();

            while (currentThread != 0)
            {
                var threadData = SOS.GetThreadData(currentThread);

                threads.Add(new SOSThreadInfo(threadData, SOS));

                currentThread = threadData.nextThread;
            }

            foreach (var thread in threads)
                WriteObject(thread);
        }
    }
}
