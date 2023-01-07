namespace DebugTools.Profiler
{
    public interface IMethodFrame : IFrame
    {
        MethodInfo MethodInfo { get; }

        IMethodFrame CloneWithNewParent(IFrame newParent);
    }
}
