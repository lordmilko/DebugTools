using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ClrDebug;

namespace DebugTools.Profiler
{
    public class ArrayValue : IValue<Array> //It's actually a matrix, not a jagged array, but we don't know what the rank will be
    {
        public CorElementType ElementType { get; }

        public int Rank { get; }

        public int[] Lengths { get; }

        public Array Value { get; } //temp

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

                var array = Array.CreateInstance(typeof(object), lengths.ToArray());

                for (var i = 0; i < Rank; i++)
                {
                    for (var j = 0; j < lengths[i]; j++)
                    {
                        array.SetValue(values[i][j], i, j);
                    }
                }

                Lengths = lengths.ToArray();
                Value = array;
            }
        }

        public override string ToString()
        {
            if (Value == null)
                return "null";

            var builder = new StringBuilder();

            builder.Append("{");

            for (var i = 0; i < Value.Rank; i++)
            {
                var length = Value.GetLength(i);

                builder.Append("{");

                for (var j = 0; j < length; j++)
                {
                    var elm = Value.GetValue(i, j);

                    builder.Append(elm);

                    if (j < length)
                        builder.Append(", ");
                }

                builder.Append("}");

                if (i < Value.Rank)
                    builder.Append(", ");
            }

            builder.Append("}");

            return builder.ToString();
        }
    }
}
