using System.Diagnostics;
using System.IO;

namespace DebugTools.PowerShell
{
    [DebuggerDisplay("{ModuleName,nq} {TypeName,nq}.{MethodName,nq}")]
    public class MethodInfo
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
    }
}