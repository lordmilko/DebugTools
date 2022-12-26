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
