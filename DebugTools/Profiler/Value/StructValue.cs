using System;
using System.IO;

namespace DebugTools.Profiler
{

    public class StructValue : ComplexTypeValue
    {
        public StructValue(BinaryReader reader, ValueSerializer serializer) : base(reader, serializer)
        {
            if (Value == null)
                throw new InvalidOperationException($"Value should not be null in {nameof(StructValue)}");
        }
    }
}
