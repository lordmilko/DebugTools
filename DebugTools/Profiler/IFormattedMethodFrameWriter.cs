namespace DebugTools.Profiler
{
    interface IFormattedMethodFrameWriter : IMethodFrameWriter
    {
        void Print(IFrame frame);
    }
}