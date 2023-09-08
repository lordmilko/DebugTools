using ClrDebug;

namespace DebugTools
{
    /// <summary>
    /// Represents a generic parameter on a member that is part of a generic type. e.g. class Foo&lt;T&gt; { void Bar(T t) {} }. The type is in fact represented as a number.
    /// </summary>
    class SigTypeGenericArgType : SigType
    {
        public int Index { get; }

        public string Name { get; }

        public SigTypeGenericArgType(CorElementType type, ref SigReader reader) : base(type)
        {
            Index = reader.CorSigUncompressData();

            if (reader.Token.Type == CorTokenType.mdtMethodDef)
            {
                Name = GetTypeGenericArgName(Index, (mdMethodDef) reader.Token, reader.Import);
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
