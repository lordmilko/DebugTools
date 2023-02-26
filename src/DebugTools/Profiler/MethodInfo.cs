using System.Diagnostics;
using System.IO;
using ClrDebug;

namespace DebugTools.Profiler
{
    /// <summary>
    /// Describes a method that is known to the profiler.
    /// </summary>
    [DebuggerDisplay("{ModuleName,nq} {TypeName,nq}.{MethodName,nq}")]
    public class MethodInfo : IMethodInfoInternal
    {
        public FunctionID FunctionID { get; }

        public string ModulePath { get; }

        public string ModuleName => Path.GetFileName(ModulePath);

        public string TypeName { get; }

        public string MethodName { get; }

        bool IMethodInfoInternal.WasUnknown { get; set; }

        internal bool WasUnknown
        {
            get => ((IMethodInfoInternal) this).WasUnknown;
            set => ((IMethodInfoInternal)this).WasUnknown = value;
        }

        public MethodInfo(FunctionID functionId, string modulePath, string typeName, string methodName)
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
