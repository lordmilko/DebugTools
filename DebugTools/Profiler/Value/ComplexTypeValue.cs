using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DebugTools.Profiler
{
    public abstract class ComplexTypeValue : IValue<object>
    {
        public string Name { get; }

        public object Value { get; }

        public List<object> FieldValues { get; }

        protected ComplexTypeValue(BinaryReader reader, ValueSerializer serializer)
        {
            Value = this;

            var length = reader.ReadInt32();

            if (length == 0)
            {
                //It's null
                Value = null;
                return;
            }

            var nameBytes = reader.ReadBytes(length * 2);
            Name = Encoding.Unicode.GetString(nameBytes, 0, (length - 1) * 2);

            var numFields = reader.ReadInt32();

            if (numFields > 0)
            {
                FieldValues = new List<object>();

                for (var i = 0; i < numFields; i++)
                {
                    var value = serializer.ReadValue();

                    FieldValues.Add(value);
                }
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
