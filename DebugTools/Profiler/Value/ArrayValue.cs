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
                var dimensionSizes = new int[Rank];

                for (var i = 0; i < Rank; i++)
                    dimensionSizes[i] = reader.ReadInt32();

                var totalLength = dimensionSizes[0];

                for (var i = 1; i < Rank; i++)
                    totalLength *= dimensionSizes[i];

                var indices = new int[Rank];

                var currentDimension = Rank - 1;

                var array = Array.CreateInstance(typeof(object), dimensionSizes);

                for (var i = 0; i < totalLength; i++)
                {
                    var current = serializer.ReadValue();

                    array.SetValue(current, indices);

                    UpdateArrayIndices(indices, ref currentDimension, dimensionSizes, Rank);

                    currentDimension = Rank - 1;
                }

                Value = array;
            }
        }

        public static void UpdateArrayIndices(int[] indices, ref int currentDimension, int[] dimensionSizes, int rank)
        {
            indices[currentDimension]++;

            while (true)
            {
                if (indices[currentDimension] >= dimensionSizes[currentDimension] && currentDimension > 0)
                {
                    //If we're now at [0,0,4], move to [0,1,4]...
                    indices[currentDimension - 1]++;

                    //...and then to [0,1,0]
                    for (var j = currentDimension; j < rank; j++)
                        indices[j] = 0;

                    //Recurse all the higher dimensions, so that when we're at
                    //[0,4,0] we can move to [1,0,0]
                    currentDimension--;
                }
                else
                    break;
            }
        }

        public override string ToString()
        {
            if (Value == null)
                return "null";

            var builder = new StringBuilder();

            builder.Append("new[");

            for (var i = 0; i < Value.Rank - 1; i++)
                builder.Append(",");

            builder.Append("]");

            var dimensionSizes = new int[Value.Rank];

            for (var i = 0; i < Value.Rank; i++)
            {
                var dimensionLength = Value.GetLength(i);
                dimensionSizes[i] = dimensionLength;

                builder.Append("{");
            }

            var totalLength = Value.Length;

            var indices = new int[Value.Rank];

            var currentDimension = Value.Rank - 1;

            for (var i = 0; i < totalLength; i++)
            {
                var current = Value.GetValue(indices);

                builder.Append(current);

                builder.Append(",");

                UpdateArrayIndices(indices, ref currentDimension, dimensionSizes, Value.Rank);

                var done = Value.Rank - 1 - currentDimension;

                if (done > 0)
                {
                    builder.Length--;

                    for (var j = 0; j < done; j++)
                    {
                        builder.Append("}");
                    }

                    builder.Append(",");

                    for (var j = 0; j < done; j++)
                    {
                        builder.Append("{");
                    }
                }

                currentDimension = Value.Rank - 1;
            }

            builder.Length -= Rank; //Get rid of Rank - 1 {'s and the last comma
            builder.Append("}");

            return builder.ToString();
        }
    }
}
