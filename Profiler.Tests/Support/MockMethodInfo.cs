using DebugTools.Profiler;

namespace Profiler.Tests
{
    class MockMethodInfo : IMethodInfo
    {
        public long FunctionID { get; }
        public string ModuleName { get; }
        public string TypeName { get; }
        public string MethodName { get; }
        public bool WasUnknown { get; set; }

        public MockMethodInfo(System.Reflection.MethodInfo methodInfo)
        {
            TypeName = methodInfo?.DeclaringType.Name;
            MethodName = methodInfo?.Name;
        }
    }
}