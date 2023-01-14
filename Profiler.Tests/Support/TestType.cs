namespace Profiler.Tests
{
    enum TestType
    {
        Value,
        Profiler,
        Exception,
        Blacklist,
        SOS
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

        PtrArg,
        PtrCharArg,
        Value_PtrCharRandomValueArg,
        PtrVoidArg,
        PtrStructArg,
        PtrComplexStructArg,

        PtrPtrArg,
        PtrPtrCharArg,
        PtrPtrVoidArg,
        PtrPtrStructArg,
        PtrPtrComplexStructArg,

        PtrArrayArg,
        PtrCharArrayArg,
        PtrVoidArrayArg,
        PtrStructArrayArg,
        PtrComplexStructArrayArg,

        FnPtr,
        FnPtrNull,

        DecimalArg,

        StringArg,
        EmptyStringArg,
        NullStringArg,

        ObjectArg,
        GenericClassArg,
        GenericTwoTypeArgs,
        GenericValueTypeArg,
        GenericClass_ToObjectArg,
        GenericValueType_ToObjectArg,
        InterfaceArg,
        NestedNestedExternalType,
        GenericWithObjectTypeArg,

        #region MethodVar

        Generic_MethodVar_ElementTypeClassArg,
        Generic_MethodVar_ElementTypeValueTypeArg,
        Generic_MethodVar_ElementTypeSimpleArg,

        #endregion
        #region MethodVar Array

        Generic_MethodVar_ElementTypeClassArrayArg,
        Generic_MethodVar_ElementTypeValueTypeArrayArg,
        Generic_MethodVar_ElementTypeSimpleArrayArg,

        #endregion
        #region MethodVar Generic Value

        Generic_MethodVar_ElementTypeGenericClassArg,
        Generic_MethodVar_ElementTypeGenericClassArrayArg,
        Generic_MethodVar_ElementTypeGenericValueTypeArg,
        Generic_MethodVar_ElementTypeGenericValueTypeArrayArg,
        Generic_MethodVar_ElementTypeGenericValueType_SimpleArg,
        Generic_MethodVar_ElementTypeNullablePrimitive,
        Generic_MethodVar_ElementTypeNullableValueType,

        Generic_MethodVar_ElementTypeGenericValueType_SZArrayValueArg,
        Generic_MethodVar_ElementTypeGenericValueType_SZArrayArg,
        Generic_MethodVar_ElementTypeGenericValueType_MultiArrayValueArg,
        Generic_MethodVar_ElementTypeGenericValueType_MultiArrayArg,

        #endregion
        #region TypeVar

        Generic_TypeVar_ElementTypeClassArg,
        Generic_TypeVar_ElementTypeValueTypeArg,
        Generic_TypeVar_ElementTypeSimpleArg,

        #endregion
        #region TypeVar Array

        Generic_TypeVar_ElementTypeClassArrayArg,
        Generic_TypeVar_ElementTypeValueTypeArrayArg,
        Generic_TypeVar_ElementTypeSimpleArrayArg,

        #endregion

        StringArrayArg,
        EmptyStringArrayArg,
        ObjectArrayContainingStringArg,

        MultiArrayThreeDimensionsArg,

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

    enum ExceptionTestType
    {
        CaughtWithinMethod,
        UnwindOneFrame,
        UnwindTwoFrames,
        Nested_ThrownInCatchAndImmediatelyCaught,
        Nested_ThrownInCatchAndCaughtByOuterCatch,
        Nested_InnerException_UnwindToInnerHandler_InDeeperFrameThanOuterCatch,

        Nested_CaughtByOuterCatch,

        //If the nested exception occurred within a catch, and escapes the catch, the original exception's processing will never resume.
        Nested_UnwindOneFrameFromThrowInCatch,
        Nested_UnwindTwoFramesFromThrowInCatch,

        Nested_ThrownInFinallyAndImmediatelyCaught,

        //If the nested exception occurred within a finally, and escapes the finally, the original exception's processing will never resume.
        Nested_ThrownInFinallyAndUnwindOneFrame,
        Nested_ThrownInFinallyAndUnwindTwoFrames,

        NoCatchThrowWithFinallyUnwindOneFrame,
        NoCatchThrowInFinallyUnwindOneFrame,

        UncaughtInNative,
        CaughtInNative,

        Rethrow,
        CallFunctionInCatchAndThrow,
        ThrownInFilterAndCaught,

        //If the nested exception occurred within a filter, and escapes the filter, the filter will be considered to return "false" and the first pass will continue.
        ThrownInFilterAndNotCaught,
        ThrownInFilterThatUnwindsOneFrameAndNotCaught
    }

    enum BlacklistTestType
    {
        Simple
    }
}
