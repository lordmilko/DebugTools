using System;
using System.IO;

namespace DebugTools.Profiler
{
    class UIntPtrValue : IValue<UIntPtr>
    {
        public UIntPtr Value { get; }

        public UIntPtrValue(BinaryReader reader)
        {
            Value = new UIntPtr(reader.ReadUInt64());
        }
    }
}
