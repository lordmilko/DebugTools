using System.IO;

namespace DebugTools.Profiler
{
    class UInt64Value : IValue<ulong>
    {
        public ulong Value { get; }

        public UInt64Value(BinaryReader reader)
        {
            Value = reader.ReadUInt64();
        }
    }
}
