namespace DebugTools.Profiler
{
    public interface IFormattedMethodFrameWriter : IMethodFrameWriter
    {
        void Print(IFrame frame);
    }
}
