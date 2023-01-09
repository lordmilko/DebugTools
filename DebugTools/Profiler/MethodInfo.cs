using System.Diagnostics;
using System.IO;

namespace DebugTools.Profiler
{
    /// <summary>
    /// Describes a method that is known to the profiler.
    /// </summary>
    [DebuggerDisplay("{ModuleName,nq} {TypeName,nq}.{MethodName,nq}")]
    public class MethodInfo : IMethodInfo
    {
        public long FunctionID { get; }

        internal string ModulePath { get; }

        public string ModuleName => Path.GetFileName(ModulePath);

        public string TypeName { get; }

        public string MethodName { get; }

        public MethodInfo(long functionId, string modulePath, string typeName, string methodName)
        {
            FunctionID = functionId;
            ModulePath = modulePath;
            TypeName = typeName;
            MethodName = methodName;
        }

        public override string ToString()
        {
            return $"{TypeName}.{MethodName}";
        }
    }
}
