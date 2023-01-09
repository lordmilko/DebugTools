using System;
using System.IO;

namespace DebugTools.Profiler
{
    public class UIntPtrValue : IValue<UIntPtr>
    {
        public UIntPtr Value { get; }

        public UIntPtrValue(BinaryReader reader)
        {
            //long will be casted to int on x86
            Value = new UIntPtr(reader.ReadUInt64());
        }

        public override string ToString()
        {
            return $"0x{Value.ToUInt64():X}";
        }
    }
}
