namespace DebugTools.Profiler
{
    public class MethodFrameStringWriter : IMethodFrameWriter
    {
        public static MethodFrameStringWriter Default => new MethodFrameStringWriter(MethodFrameFormatter.Default);

        private MethodFrameFormatter formatter;

        public IOutputSource Output { get; }

        public MethodFrameStringWriter(MethodFrameFormatter formatter)
        {
            Output = new StringOutputSource();
            this.formatter = formatter;
        }

        public IMethodFrameWriter Write(object value, IFrame frame, FrameTokenKind kind)
        {
            Output.Write(value);

            return this;
        }

        public string ToString(IFrame frame)
        {
            formatter.Format(frame, this);

            return ((StringOutputSource) Output).ToStringAndClear();
        }
    }
}
