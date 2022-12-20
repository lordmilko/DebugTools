using ClrDebug;

namespace DebugTools
{
    class SigValueType : SigType
    {
        public mdToken Token { get; }

        public string Name { get; }

        public SigValueType(CorElementType type, bool isByRef, mdToken[] modifiers, ref SigReader reader) : base(type, isByRef, modifiers)
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