using System.Collections.Generic;

namespace DebugTools.Profiler
{
    public class MethodFrame : IMethodFrame
    {
        public IFrame Parent { get; set; }

        public IMethodInfo MethodInfo { get; }

        public long Sequence { get; }

        public List<IMethodFrame> Children { get; set; } = new List<IMethodFrame>();

        public int HashCode => GetHashCode();

        public MethodFrame(IMethodInfo methodInfo, long sequence)
        {
            MethodInfo = methodInfo;
            Sequence = sequence;
        }

        protected MethodFrame(IFrame newParent, MethodFrame originalFrame)
        {
            Parent = newParent;
            MethodInfo = originalFrame.MethodInfo;
            Sequence = originalFrame.Sequence;
        }

        public IRootFrame GetRoot()
        {
            var parent = Parent;

            while (true)
            {
                if (parent is IRootFrame)
                    return (IRootFrame)parent;

                parent = parent.Parent;
            }
        }

        public virtual IMethodFrame CloneWithNewParent(IFrame newParent)
        {
            return new MethodFrame(newParent, this);
        }

        public override string ToString()
        {
            return MethodFrameStringWriter.Default.ToString(this);
        }
    }
}
