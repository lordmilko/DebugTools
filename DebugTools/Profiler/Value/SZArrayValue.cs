using System.Collections.Generic;
using System.IO;
using System.Text;
using ClrDebug;

namespace DebugTools.Profiler
{
    public class SZArrayValue : IValue<object[]>
    {
        public int Length { get; }

        public CorElementType ElementType { get; }

        public object[] Value { get; }

        public SZArrayValue(BinaryReader reader, ValueSerializer serializer)
        {
            ElementType = (CorElementType)reader.ReadByte();

            if (ElementType == CorElementType.End)
            {
                Value = null;
            }
            else
            {
                Length = reader.ReadInt32();

                var list = new List<object>();

                if (Length > 0)
                {
                    for (var i = 0; i < Length; i++)
                        list.Add(serializer.ReadValue());
                }

                Value = list.ToArray();
            }
        }

        public override string ToString()
        {
            if (Value == null)
                return "null";

            var builder = new StringBuilder();
            builder.Append("new[]{");

            for (var i = 0; i < Value.Length; i++)
            {
                builder.Append(Value[i]);

                if (i < Value.Length - 1)
                    builder.Append(", ");
            }

            builder.Append("}");

            return builder.ToString();
        }
    }
}
