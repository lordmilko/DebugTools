using System.IO;

namespace DebugTools.Profiler
{
    class FloatValue : IValue<float>
    {
        public float Value { get; }

        public FloatValue(BinaryReader reader)
        {
            Value = reader.ReadSingle();
        }
    }
}
