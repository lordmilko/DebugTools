using System.IO;
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
            ModuleName = Path.GetFileName(methodInfo.DeclaringType.Assembly.Location);
            TypeName = methodInfo.DeclaringType.FullName;
            MethodName = methodInfo.Name;
        }
    }
}
