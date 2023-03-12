using System;
using System.Linq;
using System.Management.Automation;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "DbgProfilerStackFrame")]
    public class GetDbgProfilerStackFrame : StackFrameCmdlet, IDisposable
    {
        [Parameter(Mandatory = false)]
        public int[] Sequence { get; set; }

        private FrameFilterer filter;

        protected override void BeginProcessing()
        {
            filter = new FrameFilterer(GetFrameFilterOptions());
        }

        protected override void DoProcessRecordEx()
        {
            filter.ProcessFrame(Frame);
        }

        protected override void EndProcessing()
        {
            foreach (var item in filter.GetSortedFilteredFrames())
            {
                if (MyInvocation.BoundParameters.ContainsKey(nameof(Sequence)))
                {
                    if (!Sequence.Any(s => s == item.Sequence))
                        continue;
                }

                WriteObject(item);
            }
        }

        public void Dispose()
        {
            filter?.Dispose();
        }
    }
}
