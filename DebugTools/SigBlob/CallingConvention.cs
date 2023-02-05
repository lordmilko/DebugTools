using System.Diagnostics;

namespace DebugTools
{
    [DebuggerDisplay("{ToString(),nq}")]
    public class CallingConvention
    {
        private CorHybridCallingConvention value;

        public bool IsDefault => value == CorHybridCallingConvention.DEFAULT;

        public bool IsVarArg => (value & CorHybridCallingConvention.VARARG) == CorHybridCallingConvention.VARARG;

        public bool IsC => (value & CorHybridCallingConvention.C) == CorHybridCallingConvention.C;

        public bool IsStdCall => (value & CorHybridCallingConvention.STDCALL) == CorHybridCallingConvention.STDCALL;

        public bool IsThisCall => (value & CorHybridCallingConvention.THISCALL) == CorHybridCallingConvention.THISCALL;

        public bool IsFastCall => (value & CorHybridCallingConvention.FASTCALL) == CorHybridCallingConvention.FASTCALL;

        public bool IsGeneric => (value & CorHybridCallingConvention.GENERIC) == CorHybridCallingConvention.GENERIC;

        public static implicit operator CallingConvention(CorHybridCallingConvention value)
        {
            return new CallingConvention {value = value};
        }

        public override string ToString()
        {
            return value.ToString();
        }
    }
}
