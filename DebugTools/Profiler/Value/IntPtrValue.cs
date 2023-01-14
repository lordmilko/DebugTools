using System;
using System.IO;

namespace DebugTools.Profiler
{
    public class IntPtrValue : IValue<IntPtr>
    {
        public IntPtr Value { get; }

        public IntPtrValue(BinaryReader reader)
        {
            var raw = reader.ReadInt64();

            if (IntPtr.Size == 4)
                raw &= 0x00000000ffffffff;

            //long will be casted to int on x86
            Value = new IntPtr(raw);
        }

        public override string ToString()
        {
            return $"0x{Value.ToInt64():X}";
        }
    }
}
