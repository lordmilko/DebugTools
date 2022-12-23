using System.IO;

namespace DebugTools.Profiler
{
    class ByteValue : IValue<byte>
    {
        public byte Value { get; }

        public ByteValue(BinaryReader reader)
        {
            Value = reader.ReadByte();
        }
    }
}
