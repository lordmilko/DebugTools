using ClrDebug;

namespace DebugTools.Profiler
{
    internal interface IMethodInfoInternal : IMethodInfo
    {
        bool WasUnknown { get; set; }
    }

    public interface IMethodInfo
    {
        FunctionID FunctionID { get; }

        string ModuleName { get; }

        string TypeName { get; }

        string MethodName { get; }
    }
}
