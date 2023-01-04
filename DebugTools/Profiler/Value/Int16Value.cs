using System.IO;

namespace DebugTools.Profiler
{
    class Int16Value : IValue<short>
    {
        public short Value { get; }

        public Int16Value(BinaryReader reader)
        {
            Value = reader.ReadInt16();
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
