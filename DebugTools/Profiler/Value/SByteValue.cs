using System.IO;

namespace DebugTools.Profiler
{
    public class SByteValue : IValue<sbyte>
    {
        public sbyte Value { get; }

        public SByteValue(BinaryReader reader)
        {
            Value = (sbyte) reader.ReadByte();
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
