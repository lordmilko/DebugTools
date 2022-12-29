namespace Profiler.Tests
{
    enum TestType
    {
        Value,
        Profiler
    }

    enum ValueTestType
    {
        NoArgs_ReturnVoid,
        OneArg_ReturnVoid,
        BoolArg,
        CharArg,
        ByteArg,
        SByteArg,
        Int16Arg,
        UInt16Arg,
        Int32Arg,
        UInt32Arg,
        Int64Arg,
        UInt64Arg,
        FloatArg,
        DoubleArg,
        IntPtrArg,
        UIntPtrArg,

        DecimalArg,

        StringArg,
        EmptyStringArg,
        NullStringArg,

        ObjectArg,
        Generic_MethodVar_ElementTypeClassArg,
        Generic_MethodVar_ElementTypeValueTypeArg,
        Generic_MethodVar_ElementTypeSimpleArg,
        Generic_MethodVar_ElementTypeClassArrayArg,
        Generic_MethodVar_ElementTypeValueTypeArrayArg,
        Generic_MethodVar_ElementTypeSimpleArrayArg,

        Generic_TypeVar_ElementTypeClassArg,
        Generic_TypeVar_ElementTypeValueTypeArg,
        Generic_TypeVar_ElementTypeSimpleArg,
        Generic_TypeVar_ElementTypeClassArrayArg,
        Generic_TypeVar_ElementTypeValueTypeArrayArg,
        Generic_TypeVar_ElementTypeSimpleArrayArg,

        Generic_MethodVar_ElementTypeGenericClassArg,
        Generic_MethodVar_ElementTypeGenericClassArrayArg,

        StringArrayArg,
        EmptyStringArrayArg,
        ObjectArrayContainingStringArg,

        ClassArg,
        ClassWithFieldArg,
        ClassWithPropertyArg,
        ExternalClass,

        ClassArrayArg,
        EmptyClassArrayArg,
        
        ObjectArrayArg,
        EmptyObjectArrayArg,
        ObjectArrayOfObjectArray,

        ValueTypeArrayArg,
        EmptyValueTypeArrayArg,

        StructArg,
        StructWithFieldArg,
        StructWithPropertyArg,
        ExternalStruct,

        StructArrayArg,
        ExternalStructArrayArg,
        BoxedStructArrayArg,
        BoxedExternalStructArrayArg
    }

    enum ProfilerTestType
    {
        NoArgs,
        SingleChild,
        TwoChildren
    }
}
