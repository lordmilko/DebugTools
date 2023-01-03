using ClrDebug;

namespace DebugTools.Profiler
{
    class RecursionValue
    {
        public static readonly RecursionValue ClassInstance = new RecursionValue(CorElementType.Class);

        public static readonly RecursionValue GenericInstInstance = new RecursionValue(CorElementType.GenericInst);

        public CorElementType Type { get; }

        private RecursionValue(CorElementType type)
        {
            Type = type;
        }

        public override string ToString()
        {
            return $"[Recursion {Type}]";
        }
    }
}