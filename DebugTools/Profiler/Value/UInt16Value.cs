using System.IO;

namespace DebugTools.Profiler
{
    class UInt16Value : IValue<ushort>
    {
        public ushort Value { get; }

        public UInt16Value(BinaryReader reader)
        {
            Value = reader.ReadUInt16();
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
