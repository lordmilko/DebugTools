using System.IO;

namespace DebugTools.Profiler
{
    class CharValue : IValue<char>
    {
        public char Value { get; }

        public CharValue(BinaryReader reader)
        {
            Value = reader.ReadChar();
        }
    }
}
