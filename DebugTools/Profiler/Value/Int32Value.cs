using System.IO;

namespace DebugTools.Profiler
{
    class Int32Value : IValue<int>
    {
        public int Value { get; }

        public Int32Value(BinaryReader reader)
        {
            Value = reader.ReadInt32();
        }
    }
}
