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

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Filter)]
        public string[] StringValue { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Filter)]
        public string[] TypeName { get; set; }

        private FrameFilterer filter;

        protected override void BeginProcessing()
        {
            var options = new FrameFilterOptions
            {
                Unique = Unique,
                Include = Include,
                Exclude = Exclude,
                StringValue = StringValue,
                TypeName = TypeName,
                HasFilterValue = ParameterSetName == ParameterSet.Filter
            };

            filter = new FrameFilterer(options);
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
