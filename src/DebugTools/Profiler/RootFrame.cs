using System.Collections.Generic;

namespace DebugTools.Profiler
{
    public class RootFrame : IRootFrame
    {
        public int ThreadId { get; set; }

        public string ThreadName { get; set; }

        public IFrame Parent { get; set; }

        public List<IMethodFrame> Children { get; set; } = new List<IMethodFrame>();

        public long Sequence => -1;

        public IRootFrame GetRoot() => this;

        public IRootFrame Clone()
        {
            return new RootFrame
            {
                ThreadId = ThreadId,
                ThreadName = ThreadName
            };
        }

        public override string ToString()
        {
            return MethodFrameStringWriter.Default.ToString(this);
        }
    }
}
