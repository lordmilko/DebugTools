using System.IO;

namespace DebugTools.Profiler
{
    class SByteValue : IValue<sbyte>
    {
        public sbyte Value { get; }

        public SByteValue(BinaryReader reader)
        {
            Value = (sbyte) reader.ReadByte();
        }
    }
}
