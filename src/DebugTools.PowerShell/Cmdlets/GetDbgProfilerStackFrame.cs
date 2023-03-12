using System;
using System.Management.Automation;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "DbgProfilerStackFrame")]
    public class GetDbgProfilerStackFrame : StackFrameCmdlet, IDisposable
    {
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
                WriteObject(item);
        }

        public void Dispose()
        {
            filter?.Dispose();
        }
    }
}
