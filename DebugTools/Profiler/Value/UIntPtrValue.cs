using System;
using System.IO;

namespace DebugTools.Profiler
{
    public class UIntPtrValue : IValue<UIntPtr>
    {
        public UIntPtr Value { get; }

        public UIntPtrValue(BinaryReader reader)
        {
            var raw = reader.ReadUInt64();

            if (IntPtr.Size == 4)
                raw &= 0x00000000ffffffff;

            //long will be casted to int on x86
            Value = new UIntPtr(raw);
        }

        public override string ToString()
        {
            return $"0x{Value.ToUInt64():X}";
        }
    }
}
