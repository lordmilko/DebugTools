using System;
using System.IO;

namespace DebugTools.Profiler
{
    public class IntPtrValue : IValue<IntPtr>
    {
        public IntPtr Value { get; }

        public IntPtrValue(BinaryReader reader)
        {
            //long will be casted to int on x86
            Value = new IntPtr(reader.ReadInt64());
        }

        public override string ToString()
        {
            return $"0x{Value.ToInt64():X}";
        }
    }
}
