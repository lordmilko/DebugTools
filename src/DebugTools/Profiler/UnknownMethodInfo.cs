using System.Diagnostics;

namespace DebugTools.Profiler
{
    [DebuggerDisplay("{ToString(),nq}")]
    public class UnknownMethodInfo : MethodInfo
    {
        private const string Unknown = "Unknown";

        public UnknownMethodInfo(long functionId) : base(functionId, Unknown, Unknown, Unknown)
        {
        }

        public override string ToString()
        {
            return $"0x{FunctionID:X}";
        }
    }
}
