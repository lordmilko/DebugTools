using System.Collections.Generic;

namespace DebugTools.Profiler
{
    public class RootFrame : IFrame
    {
        public int ThreadId { get; set; }

        public string ThreadName { get; set; }

        public IFrame Parent { get; set; }

        public List<IFrame> Children { get; set; } = new List<IFrame>();

        public RootFrame GetRoot() => this;

        public override string ToString()
        {
            if (ThreadName == null)
                return ThreadId.ToString();

            return $"{ThreadName} {ThreadId}";
        }
    }
}