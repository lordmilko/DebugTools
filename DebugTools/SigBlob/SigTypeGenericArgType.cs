using ClrDebug;

namespace DebugTools
{
    /// <summary>
    /// Represents a generic parameter on a member that is part of a generic type. e.g. class Foo<T> { void Bar(T t) {} }. The type is in fact represented as a number.
    /// </summary>
    class SigTypeGenericArgType : SigType
    {
        public int Index { get; }

        public string Name { get; }

        public SigTypeGenericArgType(CorElementType type, bool isByRef, mdToken[] modifiers, ref SigReader reader) : base(type, isByRef, modifiers)
        {
            Index = reader.CorSigUncompressData();
            Name = GetTypeGenericArgName(Index, reader.MethodDef, reader.Import);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}