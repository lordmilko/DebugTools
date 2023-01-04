using System.IO;
using System.Text;

namespace DebugTools.Profiler
{
    public class StringValue : IValue<string>
    {
        public int Length { get; }

        public string Value { get; }

        public StringValue(BinaryReader reader)
        {
            Length = reader.ReadInt32();

            if (Length > 0)
            {
                var stringBytes = reader.ReadBytes(Length * 2);
                Value = Encoding.Unicode.GetString(stringBytes, 0, (Length - 1) * 2); //Ignore null terminator
            }
        }

        public override string ToString()
        {
            return Value != null ? $"\"{Value}\"" : "null";
        }
    }
}
