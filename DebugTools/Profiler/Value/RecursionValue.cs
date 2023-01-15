using ClrDebug;

namespace DebugTools.Profiler
{
    public class RecursionValue
    {
        public static readonly RecursionValue ClassInstance = new RecursionValue(CorElementType.Class);

        public static readonly RecursionValue GenericInstInstance = new RecursionValue(CorElementType.GenericInst);

        public static readonly RecursionValue Array = new RecursionValue(CorElementType.Array);

        public static readonly RecursionValue SZArray = new RecursionValue(CorElementType.SZArray);

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
