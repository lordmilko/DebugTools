using System.Text;

namespace DebugTools.Profiler
{
    class MethodFrameStringWriter : IMethodFrameWriter
    {
        public static MethodFrameStringWriter Default => new MethodFrameStringWriter(MethodFrameFormatter.Default);

        private MethodFrameFormatter formatter;
        private StringBuilder builder;

        public MethodFrameStringWriter(MethodFrameFormatter formatter)
        {
            this.formatter = formatter;
        }

        public IMethodFrameWriter Write(object value, IFrame frame, FrameTokenKind kind)
        {
            builder.Append(value);

            return this;
        }

        public string ToString(IFrame frame)
        {
            builder = new StringBuilder();

            formatter.Format(frame, this);

            return builder.ToString();
        }
    }
}
