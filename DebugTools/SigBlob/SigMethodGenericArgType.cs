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

        public SigMethodGenericArgType(CorElementType type, bool isByRef, mdToken[] modifiers, ref SigReader reader) : base(type, isByRef, modifiers)
        {
            Index = reader.CorSigUncompressData();
            Name = GetMethodGenericArgName(Index, reader.MethodDef, reader.Import);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}