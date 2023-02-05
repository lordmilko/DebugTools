namespace DebugTools.Profiler
{
    public interface IUnmanagedTransitionFrame : IMethodFrame
    {
        FrameKind Kind { get; }
    }
}
