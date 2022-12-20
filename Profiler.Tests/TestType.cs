namespace Profiler.Tests
{
    enum TestType
    {
        SigBlob,
        Profiler
    }

    enum SigBlobTestType
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
        UIntPtrArg
    }

    enum ProfilerTestType
    {
        NoArgs,
        SingleChild,
        TwoChildren
    }
}