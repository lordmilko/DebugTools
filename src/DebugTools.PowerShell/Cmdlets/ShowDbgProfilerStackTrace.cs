using System;
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

        [Parameter(Mandatory = false)]
        public string[] HighlightMethod { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter ExcludeNamespace { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter IncludeSequence { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter IncludeModule { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Simple { get; set; }

        private WildcardPattern[] highlightMethodNameWildcards;

        private FrameFilterer filter;

        private IOutputSource output = new ConsoleOutputSource();

        protected override void BeginProcessing()
        {
            if (Simple)
            {
                Unique = true;
                ExcludeNamespace = true;
            }

            if (HighlightMethod != null)
                highlightMethodNameWildcards = HighlightMethod.Select(h => new WildcardPattern(h, WildcardOptions.IgnoreCase)).ToArray();

            filter = new FrameFilterer(Options);
        }

        protected override void DoProcessRecordEx()
        {
            filter.ProcessFrame(Frame, CancellationToken);
        }

        protected override void EndProcessing()
        {
            if (Session == null)
                return;

            var outputFrames = filter.GetSortedFilteredFrameRoots(CancellationToken);

            var methodFrameFormatter = new MethodFrameFormatter(ExcludeNamespace, IncludeSequence, IncludeModule);
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
