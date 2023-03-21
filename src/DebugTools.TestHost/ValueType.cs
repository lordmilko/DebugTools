using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DebugTools.TestHost
{
    class GenericValueType<TType>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Generic_TypeVar_ElementTypeClassArg(TType a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Generic_TypeVar_ElementTypeValueTypeArg(TType a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Generic_TypeVar_ElementTypeSimpleArg(TType a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Generic_TypeVar_ElementTypeClassArrayArg(TType a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Generic_TypeVar_ElementTypeValueTypeArrayArg(TType a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Generic_TypeVar_ElementTypeSimpleArrayArg(TType a)
        {
        }
    }

    class ValueType
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void NoArgs_ReturnVoid()
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void OneArg_ReturnVoid(int a)
        {
        }

        #region Primitive

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void BoolArg(bool a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void CharArg(char a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ByteArg(byte a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void SByteArg(sbyte a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Int16Arg(short a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void UInt16Arg(ushort a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Int32Arg(int a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void UInt32Arg(uint a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Int64Arg(long a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void UInt64Arg(ulong a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void FloatArg(float a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void DoubleArg(double a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void IntPtrArg(IntPtr a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void UIntPtrArg(UIntPtr a)
        {
        }

        #endregion
        #region Ptr

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void PtrArg(int* a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void PtrCharArg(char* a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void PtrCharRandomValueArg(char* a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void PtrVoidArg(void* a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void PtrStructArg(StructWithPrimitiveField* a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void PtrComplexStructArg(ComplexPtrStruct* a)
        {
        }

        #endregion
        #region PtrPtr

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void PtrPtrArg(int** a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void PtrPtrCharArg(char** value)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void PtrPtrVoidArg(void** value)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void PtrPtrStructArg(StructWithPrimitiveField** a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void PtrPtrComplexStructArg(ComplexPtrStruct** a)
        {
        }

        #endregion
        #region Ptr Array

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void PtrArrayArg(int*[] a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void PtrCharArrayArg(char*[] a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void PtrVoidArrayArg(void*[] value)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void PtrStructArrayArg(StructWithPrimitiveField*[] a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void PtrComplexStructArrayArg(ComplexPtrStruct*[] a)
        {
        }

        #endregion
        #region ByRef

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ByRef_Ref_BoxedValue(ref object a)
        {
        }

        #region Null

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ByRef_Ref_InNull_OutNull(ref string a)
        {
            a = null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ByRef_Ref_InNull_OutValue(ref string a)
        {
            a = "value";
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ByRef_Ref_InNonNullValue_OutValue(ref string a)
        {
            a = "newValue";
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ByRef_Out_Nullable_WithNull(out string a)
        {
            a = null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ByRef_Out_Nullable_WithNonNull(out string a)
        {
            a = "value";
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ByRef_Out_NonNullNullable_WithNull(out string a)
        {
            a = "newValue";
        }

        #endregion
        #region Number

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ByRef_Ref_InZero_OutZero(ref int a)
        {
            a = 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ByRef_Ref_InZero_OutValue(ref int a)
        {
            a = 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ByRef_Ref_InNonZero_OutValue(ref int a)
        {
            a = 2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ByRef_Out_Number_WithZero(out int a)
        {
            a = 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ByRef_Out_Number_WithNonZero(out int a)
        {
            a = 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ByRef_Out_NonZeroNumber_WithNonZero(out int a)
        {
            a = 2;
        }

        #endregion
        #region Ptr

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void ByRef_Ref_InPtrZero_OutZero(ref int* a)
        {
            a = (int*) 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void ByRef_Ref_InPtrZero_OutValue(ref int* a)
        {
            a = (int*) 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void ByRef_Ref_InPtrNonZero_OutValue(ref int* a)
        {
            a = (int*) 2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void ByRef_Out_Ptr_WithZero(out int* a)
        {
            a = (int*) 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void ByRef_Out_Ptr_NonWithZero(out int* a)
        {
            a = (int*) 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void ByRef_Out_NonZeroPtr_NonWithZero(out int* a)
        {
            a = (int*) 2;
        }

        #endregion
        #endregion

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void FnPtrArg(delegate*<void> a)
        {
            Debug.WriteLine(((IntPtr)a).ToInt64().ToString("X"));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void FnPtrNullArg(delegate*<void> a)
        {
            Debug.WriteLine(((IntPtr)a).ToInt64().ToString("X"));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void DecimalArg(decimal a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void StringArg(string a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ObjectArg(object a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ObjectArg_TraceDepth(object a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void InterfaceArg(IInterface a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void NestedNestedExternalType(Dictionary<string, int>.ValueCollection.Enumerator a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void GenericWithObjectTypeArg(GenericValueTypeType<object> a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void GenericClassArg(GenericClassType<Class1WithField> a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void GenericValueTypeArg(GenericValueTypeType<StructWithPrimitiveField> a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void GenericTwoTypeArgs(Dictionary<string, int> a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void GenericClass_ToObjectArg(object a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void GenericValueType_ToObjectArg(object a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Generic_MethodVar_ElementTypeClassArg<TMethod>(TMethod a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Generic_MethodVar_ElementTypeValueTypeArg<TMethod>(TMethod a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Generic_MethodVar_ElementTypeSimpleArg<TMethod>(TMethod a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Generic_MethodVar_ElementTypeClassArrayArg<TMethod>(TMethod[] a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Generic_MethodVar_ElementTypeValueTypeArrayArg<TMethod>(TMethod[] a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Generic_MethodVar_ElementTypeSimpleArrayArg<TMethod>(TMethod[] a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Generic_MethodVar_ElementTypeGenericClassArg<TMethod>(GenericClassType<TMethod> a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Generic_MethodVar_ElementTypeGenericClassArrayArg<TMethod>(GenericClassType<TMethod> a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Generic_MethodVar_ElementTypeGenericValueTypeArg<TMethod>(GenericValueTypeType<TMethod> a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Generic_MethodVar_ElementTypeGenericValueTypeArrayArg<TMethod>(GenericValueTypeType<TMethod> a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Generic_MethodVar_ElementTypeGenericValueType_SimpleArg<TMethod>(GenericValueTypeType<TMethod> a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Generic_MethodVar_ElementTypeNullablePrimitive(int? a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Generic_MethodVar_ElementTypeNullableValueType(DateTime? a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Generic_MethodVar_ElementTypeGenericValueType_SZArrayValueArg<TMethod>(GenericValueTypeType<TMethod>[] a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Generic_MethodVar_ElementTypeGenericValueType_SZArrayArg<TMethod>(GenericValueTypeType<TMethod[]> a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Generic_MethodVar_ElementTypeGenericValueType_MultiArrayValueArg<TMethod>(GenericValueTypeType<TMethod>[,] a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Generic_MethodVar_ElementTypeGenericValueType_MultiArrayArg<TMethod>(GenericValueTypeType<TMethod[,]> a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void EmptyStringArg(string a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void NullStringArg(string a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void StringArrayArg(string[] a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void StringArrayArg_TraceDepth(string[] a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void EmptyStringArrayArg(string[] a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ObjectArrayContainingStringArg(object[] a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void MultiArrayThreeDimensionsArg(int[,,] a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ClassArg(Class1 a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ClassWithFieldArg(Class1WithField a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ClassWithPropertyArg(Class1WithProperty a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ExternalClass(Uri a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ClassArrayArg(Class1[] a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void EmptyClassArrayArg(Class1[] a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ObjectArrayArg(object[] a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void EmptyObjectArrayArg(object[] a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ObjectArrayOfObjectArray(object[] a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ValueTypeArrayArg(int[] a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void EmptyValueTypeArrayArg(int[] a)
        {
        }

        #region Struct

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void StructArg(Struct1 a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void StructWithPrimitiveFieldArg(StructWithPrimitiveField a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void StructWithReferenceFieldArg(StructWithReferenceField a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void StructWithPrimitivePropertyArg(StructWithPrimitiveProperty a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void StructWithReferencePropertyArg(StructWithReferenceProperty a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ExternalStruct(DateTime a)
        {
        }

        #endregion
        #region Struct Array

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void StructWithPrimitiveFieldArrayArg(StructWithPrimitiveField[] a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void StructWithReferenceFieldArrayArg(StructWithReferenceField[] a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void StructWithPrimitivePropertyArrayArg(StructWithPrimitiveProperty[] a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void StructWithReferencePropertyArrayArg(StructWithReferenceProperty[] a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ExternalStructArrayArg(DateTime[] a)
        {
        }

        #endregion
        #region Boxed Struct Array

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void BoxedStructWithPrimitiveFieldArrayArg(object[] a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void BoxedStructWithReferenceFieldArrayArg(object[] a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void BoxedStructWithPrimitivePropertyArrayArg(object[] a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void BoxedStructWithReferencePropertyArrayArg(object[] a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void BoxedStructAndStringArrayArg(object[] a)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void BoxedExternalStructAndStringArrayArg(object[] a)
        {
        }

        #endregion

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void VarArg1(string a, __arglist)
        {
            VarArg2("first", __arglist(2, true, "three"));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void VarArg2(string a, __arglist)
        {
        }
    }
}
