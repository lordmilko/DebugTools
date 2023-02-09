using System;
using System.Management.Automation;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Find, "DbgProfilerStackFrame")]
    public class FindDbgProfilerStackFrame : StackFrameCmdlet, IDisposable
    {
        [Parameter(Mandatory = false)]
        public SwitchParameter Unique { get; set; }

        [Parameter(Mandatory = false, Position = 0)]
        public string[] Include { get; set; }

        [Parameter(Mandatory = false)]
        public string[] Exclude { get; set; }

        private FrameFilterer filter;

        protected override void BeginProcessing()
        {
            filter = new FrameFilterer(GetFrameFilterOptions(Unique, Include, Exclude));
        }

        protected override void DoProcessRecordEx()
        {
            filter.ProcessFrame(Frame);
        }

        protected override void EndProcessing()
        {
            foreach (var item in filter.SortedFrames)
                WriteObject(item);
        }

        public void Dispose()
        {
            filter?.Dispose();
        }
    }
}
