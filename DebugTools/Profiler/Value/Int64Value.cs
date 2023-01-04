using System.IO;

namespace DebugTools.Profiler
{
    class Int64Value : IValue<long>
    {
        public long Value { get; }

        public Int64Value(BinaryReader reader)
        {
            Value = reader.ReadInt64();
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
