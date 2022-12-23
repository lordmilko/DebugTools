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
        StringArrayArg,
        EmptyStringArrayArg,

        ClassArg,
        ClassArrayArg,
        EmptyClassArrayArg,
        ObjectArrayContainingStringArg,
        ObjectArrayArg,
        EmptyObjectArrayArg
    }

    enum ProfilerTestType
    {
        NoArgs,
        SingleChild,
        TwoChildren
    }
}
