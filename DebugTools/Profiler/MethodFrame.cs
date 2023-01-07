using System.Collections.Generic;

namespace DebugTools.Profiler
{
    public class MethodFrame : IFrame
    {
        public IFrame Parent { get; set; }

        public MethodInfo MethodInfo { get; }

        public long Sequence { get; }

        public List<MethodFrame> Children { get; set; } = new List<MethodFrame>();

        public int HashCode => GetHashCode();

        public MethodFrame(MethodInfo methodInfo, long sequence)
        {
            MethodInfo = methodInfo;
            Sequence = sequence;
        }

        public MethodFrame(IFrame newParent, MethodFrame originalFrame)
        {
            Parent = newParent;
            MethodInfo = originalFrame.MethodInfo;
            Sequence = originalFrame.Sequence;
        }

        public RootFrame GetRoot()
        {
            var parent = Parent;

            while (true)
            {
                if (parent is RootFrame)
                    return (RootFrame)parent;

                parent = parent.Parent;
            }
        }

        public override string ToString()
        {
            return MethodFrameStringWriter.Default.ToString(this);
        }
    }
}
