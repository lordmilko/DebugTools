using ClrDebug;

namespace DebugTools
{
    /// <summary>
    /// Represents a Single dimensional Zero based array (indices start at zero). This is _not_ a "string zero" array - the "SZ" here has nothing to do with strings, as it would in hungarian notation.
    /// </summary>
    class SigSZArrayType : SigType
    {
        public SigType ElementType { get; }

        public SigSZArrayType(CorElementType type, ref SigReader reader) : base(type)
        {
            ElementType = New(ref reader);
        }

        public override string ToString()
        {
            return $"{ElementType}[]";
        }
    }
}
