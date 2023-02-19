using System.IO;

namespace DebugTools.Profiler
{
    public class UInt64Value : IValue<ulong>
    {
        public ulong Value { get; }

        public UInt64Value(BinaryReader reader)
        {
            Value = reader.ReadUInt64();
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
