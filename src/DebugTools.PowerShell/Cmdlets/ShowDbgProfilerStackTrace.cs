using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using DebugTools.Profiler;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Show, "DbgProfilerStackTrace")]
    public class ShowDbgProfilerStackTrace : StackFrameCmdlet, IDisposable
    {
        [Parameter(Mandatory = false)]
        public int Depth { get; set; } = 10;

        [Parameter(Mandatory = false)]
        public SwitchParameter Unlimited { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Filter)]
        public SwitchParameter Unique { get; set; }

        [Parameter(Mandatory = false, Position = 0, ParameterSetName = ParameterSet.Filter)]
        public string[] Include { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Filter)]
        public string[] Exclude { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Filter)]
        public string[] CalledFrom { get; set; }

        [Parameter(Mandatory = false)]
        public string[] HighlightMethod { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter ExcludeNamespace { get; set; }

        private WildcardPattern[] highlightMethodNameWildcards;

        private List<IFrame> frames = new List<IFrame>();

        private FrameFilterer filter;

        private IOutputSource output = new ConsoleOutputSource();

        protected override void BeginProcessing()
        {
            if (HighlightMethod != null)
                highlightMethodNameWildcards = HighlightMethod.Select(h => new WildcardPattern(h, WildcardOptions.IgnoreCase)).ToArray();

            if (ParameterSetName == ParameterSet.Filter)
            {
                filter = new FrameFilterer(
                    GetFrameFilterOptions(Unique, Include, Exclude, CalledFrom)
                );
            }
        }

        protected override void DoProcessRecordEx()
        {
            if (filter != null)
                filter.ProcessFrame(Frame);
            else
                frames.Add(Frame);
        }

        protected override void EndProcessing()
        {
            List<IFrame> outputFrames;

            if (filter != null)
                outputFrames = filter.GetSortedFilteredFrames();
            else
                outputFrames = frames;

            var methodFrameFormatter = new MethodFrameFormatter(ExcludeNamespace);
            var methodFrameWriter = new MethodFrameColorWriter(methodFrameFormatter, output, Session.Modules)
            {
                HighlightValues = filter?.MatchedValues,
                HighlightMethodNames = highlightMethodNameWildcards,
                HighlightFrames = filter?.HighlightFrames
            };

            var stackWriter = new StackFrameWriter(
                methodFrameWriter,
                GetDepth(),
                CancellationToken
            );

            stackWriter.Execute(outputFrames);

            base.EndProcessing();
        }

        private int? GetDepth()
        {
            if (Unlimited || ParameterSetName == ParameterSet.Filter)
                return null;

            return Depth;
        }

        public void Dispose()
        {
            filter?.Dispose();
        }
    }
}
