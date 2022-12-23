using System.IO;

namespace DebugTools.Profiler
{
    class ValueType : IValue<object>
    {
        public object Value { get; }

        public ValueType(BinaryReader reader)
        {
            throw new System.NotImplementedException();
        }
    }
}
