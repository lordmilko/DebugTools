using System.Collections.Generic;
using System.IO;
using ClrDebug;

namespace DebugTools.Profiler
{
    public class SZArrayValue : IValue<object[]>
    {
        public int Length { get; }

        public CorElementType ElementType { get; }

        public object[] Value { get; } //temp

        public SZArrayValue(BinaryReader reader, ValueSerializer serializer)
        {
            ElementType = (CorElementType)reader.ReadByte();
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
}
