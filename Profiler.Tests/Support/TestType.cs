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
        GenericClassArg,
        GenericTwoTypeArgs,
        GenericValueTypeArg,
        GenericClass_ToObjectArg,
        GenericValueType_ToObjectArg,
        InterfaceArg,
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
