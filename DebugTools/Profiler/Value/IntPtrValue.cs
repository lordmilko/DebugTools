using System;
using System.IO;

namespace DebugTools.Profiler
{
    class IntPtrValue : IValue<IntPtr>
    {
        public IntPtr Value { get; }

        public IntPtrValue(BinaryReader reader)
        {
            Value = new IntPtr(reader.ReadInt64());
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
