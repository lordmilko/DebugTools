using ClrDebug;

namespace DebugTools
{
    class SigClassType : SigType
    {
        public string Name { get; }

        public mdToken Token { get; }

        public SigClassType(CorElementType type, bool isByRef, mdToken[] modifiers, ref SigReader reader) : base(type, isByRef, modifiers)
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