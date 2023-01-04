namespace DebugTools.Profiler
{
    public interface IMethodFrameWriter
    {
        IMethodFrameWriter Write(object value, IFrame frame, FrameTokenKind kind);
    }
}
