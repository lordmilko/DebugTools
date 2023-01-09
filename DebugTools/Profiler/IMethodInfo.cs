namespace DebugTools.Profiler
{
    public interface IMethodInfo
    {
        long FunctionID { get; }

        string ModuleName { get; }

        string TypeName { get; }

        string MethodName { get; }
    }
}
