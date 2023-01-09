using System.IO;

namespace DebugTools.Profiler
{
    public class UInt32Value : IValue<uint>
    {
        public uint Value { get; }

        public UInt32Value(BinaryReader reader)
        {
            Value = reader.ReadUInt32();
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
