using ClrDebug;

namespace DebugTools
{
    public class SigRefType : SigType
    {
        public SigType InnerType { get; }

        public SigRefType(ref SigReader reader) : base(CorElementType.ByRef)
        {
            InnerType = New(ref reader);
        }

        public override string ToString()
        {
            return $"{InnerType}&";
        }
    }
}
