using ClrDebug;

namespace DebugTools
{
    class SigClassType : SigType
    {
        public string Name { get; }

        public mdToken Token { get; }

        public SigClassType(CorElementType type, ref SigReader reader) : base(type)
        {
            Token = reader.CorSigUncompressToken();
            Name = GetName(Token, reader.Import);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
