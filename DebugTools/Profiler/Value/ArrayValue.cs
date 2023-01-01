using System.Collections.Generic;
using System.IO;
using ClrDebug;

namespace DebugTools.Profiler
{
    public class ArrayValue : IValue<object[][]> //It's actually a matrix, not a jagged array, but we don't know what the rank will be
    {
        public CorElementType ElementType { get; }

        public int Rank { get; }

        public int[] Lengths { get; }

        public object[][] Value { get; } //temp

        public ArrayValue(BinaryReader reader, ValueSerializer serializer)
        {
            Rank = reader.ReadInt32();
            ElementType = (CorElementType)reader.ReadByte();

            if (ElementType == CorElementType.End)
            {
                Value = null;
            }
            else
            {
                var lengths = new List<int>();
                var values = new List<object[]>();

                for(var i = 0; i < Rank; i++)
                {
                    var length = reader.ReadInt32();

                    var list = new List<object>();

                    if (length > 0)
                    {
                        for (var j = 0; j < length; j++)
                            list.Add(serializer.ReadValue());
                    }

                    var value = list.ToArray();

                    lengths.Add(length);
                    values.Add(value);
                }

                Lengths = lengths.ToArray();
                Value = values.ToArray();
            }
        }
    }
}