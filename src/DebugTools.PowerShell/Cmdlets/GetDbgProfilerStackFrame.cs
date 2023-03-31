using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using DebugTools.Profiler;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "DbgProfilerStackFrame")]
    public class GetDbgProfilerStackFrame : StackFrameCmdlet, IDisposable
    {
        [Parameter(Mandatory = false)]
        public int[] Sequence { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Roots { get; set; }

        private FrameFilterer filter;

        protected override void BeginProcessing()
        {
            filter = new FrameFilterer(GetFrameFilterOptions());
        }

        protected override void DoProcessRecordEx()
        {
            filter.ProcessFrame(Frame, CancellationToken);
        }

        protected override void EndProcessing()
        {
            IEnumerable<IFrame> frames = Roots
                ? filter.GetSortedFilteredFrameRoots(CancellationToken)
                : filter.GetSortedFilteredFrames();

            foreach (var item in frames)
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
