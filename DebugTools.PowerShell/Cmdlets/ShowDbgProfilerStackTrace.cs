using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
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

        [Parameter(Mandatory = false)]
        public string[] Highlight { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Filter)]
        public string[] StringValue { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Filter)]
        public string[] TypeName { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter ExcludeNamespace { get; set; }

        private WildcardPattern[] highlightMethodNameWildcards;
        private List<IFrame> highlightFrames = new List<IFrame>();

        private List<IFrame> frames = new List<IFrame>();

        private FrameFilterer filter;

        private MethodFrameColorWriter methodFrameWriter;

        private IOutputSource output = new ConsoleOutputSource();

        protected override void BeginProcessing()
        {
            if (Highlight != null)
                highlightMethodNameWildcards = Highlight.Select(h => new WildcardPattern(h, WildcardOptions.IgnoreCase)).ToArray();

            var methodFrameFormatter = new MethodFrameFormatter(ExcludeNamespace);
            methodFrameWriter = new MethodFrameColorWriter(methodFrameFormatter, output);

            if (ParameterSetName == ParameterSet.Filter)
            {
                filter = new FrameFilterer(
                    new FrameFilterOptions
                    {
                        Include = Include,
                        Exclude = Exclude,
                        Unique = Unique,
                        StringValue = StringValue,
                        TypeName = TypeName,
                        HasFilterValue = ParameterSetName == ParameterSet.Filter
                    }
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
            var outputFrames = filter.GetSortedMaybeValueFilteredFrames();

            methodFrameWriter.HighlightValues = filter.MatchedValues;
            methodFrameWriter.HighlightMethodNames = highlightMethodNameWildcards;
            methodFrameWriter.HighlightFrames = filter.HighlightFrames;

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
