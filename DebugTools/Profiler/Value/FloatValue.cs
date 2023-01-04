using System.Globalization;
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

        public override string ToString()
        {
            return Value.ToString(CultureInfo.CurrentCulture);
        }
    }
}
