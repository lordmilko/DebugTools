namespace DebugTools.Profiler
{
    public interface IMethodFrame : IFrame
    {
        IMethodInfo MethodInfo { get; }

        IMethodFrame CloneWithNewParent(IFrame newParent);
    }
}
