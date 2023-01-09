using System.IO;

namespace DebugTools.Profiler
{
    public class CharValue : IValue<char>
    {
        public char Value { get; }

        public CharValue(BinaryReader reader)
        {
            Value = reader.ReadChar();
        }

        public override string ToString()
        {
            return $"'{Value}'";
        }
    }
}
