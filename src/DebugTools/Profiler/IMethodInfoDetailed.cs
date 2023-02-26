namespace DebugTools.Profiler
{
    internal interface IMethodInfoDetailed : IMethodInfo
    {
        SigMethodDef SigMethod { get; }
    }
}
