using ClrDebug;

namespace DebugTools
{
    /// <summary>
    /// Represents a generic parameter in generic method definition. The type is in fact represented as a number.
    /// </summary>
    class SigMethodGenericArgType : SigType
    {
        public string Name { get; }

        public int Index { get; }

        public SigMethodGenericArgType(CorElementType type, ref SigReader reader) : base(type)
        {
            Index = reader.CorSigUncompressData();

            if (reader.Token.Type == CorTokenType.mdtMethodDef)
            {
                Name = GetMethodGenericArgName(Index, (mdMethodDef) reader.Token, reader.Import);
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
