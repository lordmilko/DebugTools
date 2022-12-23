using System.IO;

namespace DebugTools.Profiler
{
    class DoubleValue : IValue<double>
    {
        public double Value { get; }

        public DoubleValue(BinaryReader reader)
        {
            Value = reader.ReadDouble();
        }
    }
}
