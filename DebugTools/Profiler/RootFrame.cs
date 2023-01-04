using System.Collections.Generic;

namespace DebugTools.Profiler
{
    public class RootFrame : IFrame
    {
        public int ThreadId { get; set; }

        public string ThreadName { get; set; }

        public IFrame Parent { get; set; }

        public List<MethodFrame> Children { get; set; } = new List<MethodFrame>();

        public RootFrame GetRoot() => this;

        public override string ToString()
        {
            return MethodFrameStringWriter.Default.ToString(this);
        }
    }
}
