using System.IO;

namespace DebugTools.Profiler
{
    public class BoolValue : IValue<bool>
    {
        public bool Value { get; }

        public BoolValue(BinaryReader reader)
        {
            Value = reader.ReadByte() == 1;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
