using System.Collections.Generic;
using System.Threading;
using DebugTools.Profiler;

namespace DebugTools.PowerShell
{
    class StackFrameWriter
    {
        private IFormattedMethodFrameWriter writer;

        private CancellationToken cancellationToken;

        private int? depth;

        public StackFrameWriter(
            IFormattedMethodFrameWriter writer,
            int? depth,
            CancellationToken cancellationToken)
        {
            this.writer = writer;
            this.depth = depth;
            this.cancellationToken = cancellationToken;
        }

        public void Execute(List<IFrame> frames)
        {
            foreach (var frame in frames)
                Print(frame, 0, string.Empty, false, true);
        }

        private void Print(IFrame item, int level, string indent, bool last, bool first = false)
        {
            cancellationToken.ThrowIfCancellationRequested();

            writer.Output.Write(indent);

            if (!first)
            {
                if (last)
                {
                    writer.Output.Write("└─");
                    indent += "  ";
                }
                else
                {
                    writer.Output.Write("├─");
                    indent += "│ ";
                }
            }

            writer.Print(item);

            writer.Output.WriteLine();

            IList<IMethodFrame> children = item.Children;

            if (depth == null || level < depth || children.Count == 0)
            {
                for (var i = 0; i < children.Count; i++)
                    Print(children[i], level + 1, indent, i == children.Count - 1);
            }
            else
            {
                writer.Output.Write(indent);
                writer.Output.Write("└─...");
            }
        }
    }
}
