using ClrDebug;

namespace DebugTools
{
    public class SigModType : SigType
    {
        public mdToken Token { get; }

        public string Name { get; }

        public SigType InnerType { get; }

        public SigModType(CorElementType type, ref SigReader reader) : base(type)
        {
            Token = reader.CorSigUncompressToken();
            Name = GetName(Token, reader.Import);
            InnerType = New(ref reader);
        }

        public override string ToString()
        {
            return $"{Name} {InnerType}";
        }
    }
}
