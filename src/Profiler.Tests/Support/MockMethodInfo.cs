using System.IO;
using ClrDebug;
using DebugTools.Profiler;

namespace Profiler.Tests
{
    class MockMethodInfo : IMethodInfo
    {
        public FunctionID FunctionID { get; }
        public string ModulePath { get; }
        public string ModuleName => Path.GetFileName(ModulePath);
        public string TypeName { get; }
        public string MethodName { get; }
        public bool WasUnknown { get; set; }

        public MockMethodInfo(System.Reflection.MethodInfo methodInfo)
        {
            ModulePath = methodInfo.DeclaringType.Assembly.Location;
            TypeName = methodInfo.DeclaringType.FullName;
            MethodName = methodInfo.Name;
        }
    }
}
