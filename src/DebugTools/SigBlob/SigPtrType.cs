using ClrDebug;

namespace DebugTools
{
    class SigPtrType : SigType
    {
        public SigType PtrType { get; }

        public SigPtrType(CorElementType type, ref SigReader reader) : base(type)
        {
            PtrType = New(ref reader);
        }

        public override string ToString()
        {
            return $"{PtrType}*";
        }
    }
}
