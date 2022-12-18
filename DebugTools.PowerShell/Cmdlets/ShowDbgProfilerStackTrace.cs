using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using DebugTools.Profiler;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Show, "DbgProfilerStackTrace")]
    public class ShowDbgProfilerStackTrace : StackFrameCmdlet
    {
        [Parameter(Mandatory = false)]
        public int Depth { get; set; } = 10;

        [Parameter(Mandatory = false)]
        public SwitchParameter Unlimited { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Unique { get; set; }

        [Parameter(Mandatory = false, Position = 0)]
        public string[] Include { get; set; }

        [Parameter(Mandatory = false)]
        public string[] Exclude { get; set; }

        [Parameter(Mandatory = false)]
        public string[] Highlight { get; set; }

        private WildcardPattern[] highlight;
        private List<IFrame> highlightFrames = new List<IFrame>();

        private List<IFrame> frames = new List<IFrame>();

        private FindDbgProfilerStackFrame findDbgProfilerStackFrame;

        protected override void BeginProcessing()
        {
            if (Highlight != null)
                highlight = Highlight.Select(h => new WildcardPattern(h, WildcardOptions.IgnoreCase)).ToArray();

            if (Include != null)
            {
                findDbgProfilerStackFrame = new FindDbgProfilerStackFrame
                {
                    Include = Include,
                    Exclude = Exclude,
                    Unique = Unique
                };

                findDbgProfilerStackFrame.Begin();
            }
        }

        protected override void DoProcessRecordEx()
        {
            if (findDbgProfilerStackFrame != null)
            {
                findDbgProfilerStackFrame.Frame = Frame;
                findDbgProfilerStackFrame.Process();
            }
            else
                frames.Add(Frame);
        }

        protected override void EndProcessing()
        {
            if (findDbgProfilerStackFrame != null)
                frames = findDbgProfilerStackFrame.Frames;

            if (frames.All(f => !(f is RootFrame)))
            {
                ProcessFilteredFrames();
            }
            else
            {
                foreach(var frame in frames)
                    Print(frame, 0, string.Empty, false, true);
            }

            base.EndProcessing();
        }

        private void ProcessFilteredFrames()
        {
            Unlimited = true;

            var knownOriginalFrames = new Dictionary<IFrame, IFrame>();

            var newRoots = new List<RootFrame>();

            foreach (var frame in frames)
            {
                var originalFrames = GetOriginalFrames(frame);

                var newRoot = GetNewFrames(originalFrames, knownOriginalFrames);

                if (newRoot != null)
                    newRoots.Add(newRoot);
            }

            foreach (var root in newRoots)
                Print(root, 0, string.Empty, false, true);
        }

        private List<IFrame> GetOriginalFrames(IFrame frame)
        {
            var list = new List<IFrame> { frame };

            var parent = frame;

            while (!(parent is RootFrame))
            {
                parent = parent.Parent;
                list.Add(parent);
            }

            list.Reverse();

            return list;
        }

        private RootFrame GetNewFrames(List<IFrame> originalFrames, Dictionary<IFrame, IFrame> knownOriginalFrames)
        {
            IFrame newParent = null;
            RootFrame newRoot = null;

            foreach (var item in originalFrames)
            {
                if (!knownOriginalFrames.TryGetValue(item, out var newItem))
                {
                    if (item is RootFrame r)
                    {
                        newRoot = new RootFrame
                        {
                            ThreadId = r.ThreadId,
                            ThreadName = r.ThreadName
                        };

                        newItem = newRoot;
                        newParent = newRoot;
                    }
                    else if (item is MethodFrame m)
                    {
                        newItem = new MethodFrame
                        {
                            Parent = newParent,
                            MethodInfo = m.MethodInfo
                        };
                        newParent.Children.Add((MethodFrame) newItem);
                        newParent = newItem;
                    }

                    knownOriginalFrames[item] = newItem;
                }
                else
                    newParent = newItem;

                if (frames.Contains(item, FrameEqualityComparer.Instance))
                    highlightFrames.Add(newItem);
            }

            return newRoot;
        }

        private void Print(IFrame item, int level, string indent, bool last, bool first = false)
        {
            CancellationToken.ThrowIfCancellationRequested();

            Console.Write(indent);

            if (!first)
            {
                if (last)
                {
                    Console.Write("└─");
                    indent += "  ";
                }
                else
                {
                    Console.Write("├─");
                    indent += "│ ";
                }
            }

            var str = item.ToString();

            if (ShouldHighlight(str) || highlightFrames.Contains(item))
                WriteColor(str, ConsoleColor.Green);
            else
                Console.WriteLine(str);

            IList<MethodFrame> children = item.Children;

            if (level < Depth || Unlimited || children.Count == 0)
            {
                for (var i = 0; i < children.Count; i++)
                    Print(children[i], level + 1, indent, i == children.Count - 1);
            }
            else
            {
                Console.Write(indent);
                Console.Write("└─...");
            }
        }

        private bool ShouldHighlight(string str)
        {
            if (Highlight == null)
                return false;

            return highlight.Any(h => h.IsMatch(str));
        }

        private void WriteColor(string message, ConsoleColor color)
        {
            ConsoleColor fg = Host.UI.RawUI.ForegroundColor;

            Host.UI.RawUI.ForegroundColor = color;

            try
            {
                Console.WriteLine(message);
            }
            finally
            {
                Host.UI.RawUI.ForegroundColor = fg;
            }
        }
    }
}