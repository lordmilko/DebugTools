using System.IO;

namespace DebugTools.Profiler
{
    public class FnPtrValue : IValue<ulong>
    {
        /// <summary>
        /// Gets the address of the function pointer.
        /// </summary>
        public ulong Value { get; }

        public FnPtrValue(BinaryReader reader)
        {
            Value = reader.ReadUInt64();
        }

        public override string ToString()
        {
            return $"0x{Value:X}";
        }
    }
}
