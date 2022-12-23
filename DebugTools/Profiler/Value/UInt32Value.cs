using System.IO;

namespace DebugTools.Profiler
{
    class UInt32Value : IValue<uint>
    {
        public uint Value { get; }

        public UInt32Value(BinaryReader reader)
        {
            Value = reader.ReadUInt32();
        }
    }
}
