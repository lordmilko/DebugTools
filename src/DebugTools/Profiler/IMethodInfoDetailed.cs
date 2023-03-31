using ClrDebug;

namespace DebugTools.Profiler
{
    internal interface IMethodInfoDetailed : IMethodInfo
    {
        mdMethodDef Token { get; }

        SigMethodDef SigMethod { get; }
    }
}
