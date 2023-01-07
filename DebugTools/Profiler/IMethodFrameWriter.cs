namespace DebugTools.Profiler
{
    public interface IMethodFrameWriter
    {
        IOutputSource Output { get; }

        IMethodFrameWriter Write(object value, IFrame frame, FrameTokenKind kind);
    }
}
