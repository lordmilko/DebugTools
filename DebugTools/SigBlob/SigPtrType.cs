using ClrDebug;

namespace DebugTools
{
    class SigPtrType : SigType
    {
        public SigType PtrType { get; }

        public SigPtrType(CorElementType type, bool isByRef, mdToken[] modifiers, ref SigReader reader) : base(type, isByRef, modifiers)
        {
            PtrType = New(ref reader);
        }

        public override string ToString()
        {
            return $"{PtrType}*";
        }
    }
}