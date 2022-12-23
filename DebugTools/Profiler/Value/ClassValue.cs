using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DebugTools.Profiler
{
    public class ClassValue : IValue<object>
    {
        public string Name { get; }

        public object Value { get; }

        public List<object> FieldValues { get; }

        public ClassValue(BinaryReader reader, ValueSerializer serializer)
        {
            Value = this;

            var length = reader.ReadInt32();
            var nameBytes = reader.ReadBytes(length * 2);
            Name = Encoding.Unicode.GetString(nameBytes, 0, (length - 1) * 2);

            var numFields = reader.ReadInt32();

            if (numFields > 0)
            {
                FieldValues = new List<object>();

                for (var i = 0; i < numFields; i++)
                    FieldValues.Add(serializer.ReadValue());
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
