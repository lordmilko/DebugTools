using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DebugTools.TestHost
{
    class GenericValueType<TType>
    {
        public void Generic_TypeVar_ElementTypeClassArg(TType a)
        {
        }

        public void Generic_TypeVar_ElementTypeValueTypeArg(TType a)
        {
        }

        public void Generic_TypeVar_ElementTypeSimpleArg(TType a)
        {
        }

        public void Generic_TypeVar_ElementTypeClassArrayArg(TType a)
        {
        }

        public void Generic_TypeVar_ElementTypeValueTypeArrayArg(TType a)
        {
        }

        public void Generic_TypeVar_ElementTypeSimpleArrayArg(TType a)
        {
        }
    }

    class ValueType
    {
        public void NoArgs_ReturnVoid()
        {
        }

        public void OneArg_ReturnVoid(int a)
        {
        }

        #region Primitive

        public void BoolArg(bool a)
        {
        }

        public void CharArg(char a)
        {
        }

        public void ByteArg(byte a)
        {
        }

        public void SByteArg(sbyte a)
        {
        }

        public void Int16Arg(short a)
        {
        }

        public void UInt16Arg(ushort a)
        {
        }

        public void Int32Arg(int a)
        {
        }

        public void UInt32Arg(uint a)
        {
        }

        public void Int64Arg(long a)
        {
        }

        public void UInt64Arg(ulong a)
        {
        }

        public void FloatArg(float a)
        {
        }

        public void DoubleArg(double a)
        {
        }

        public void IntPtrArg(IntPtr a)
        {
        }

        public void UIntPtrArg(UIntPtr a)
        {
        }

        #endregion
        #region Ptr

        public unsafe void PtrArg(int* a)
        {
        }

        public unsafe void PtrCharArg(char* a)
        {
        }

        public unsafe void PtrCharRandomValueArg(char* a)
        {
        }

        public unsafe void PtrVoidArg(void* a)
        {
        }

        public unsafe void PtrStructArg(Struct1WithField* a)
        {
        }

        public unsafe void PtrComplexStructArg(ComplexPtrStruct* a)
        {
        }

        #endregion
        #region PtrPtr

        public unsafe void PtrPtrArg(int** a)
        {
        }

        public unsafe void PtrPtrCharArg(char** value)
        {
        }

        public unsafe void PtrPtrVoidArg(void** value)
        {
        }

        public unsafe void PtrPtrStructArg(Struct1WithField** a)
        {
        }

        public unsafe void PtrPtrComplexStructArg(ComplexPtrStruct** a)
        {
        }

        #endregion
        #region Ptr Array

        public unsafe void PtrArrayArg(int*[] a)
        {
        }

        public unsafe void PtrCharArrayArg(char*[] a)
        {
        }

        public unsafe void PtrVoidArrayArg(void*[] value)
        {
        }

        public unsafe void PtrStructArrayArg(Struct1WithField*[] a)
        {
        }

        public unsafe void PtrComplexStructArrayArg(ComplexPtrStruct*[] a)
        {
        }

        #endregion
        #region ByRef

        public void ByRef_Ref_BoxedValue(ref object a)
        {
        }

        #region Null

        public void ByRef_Ref_InNull_OutNull(ref string a)
        {
            a = null;
        }

        public void ByRef_Ref_InNull_OutValue(ref string a)
        {
            a = "value";
        }

        public void ByRef_Ref_InNonNullValue_OutValue(ref string a)
        {
            a = "newValue";
        }

        public void ByRef_Out_Nullable_WithNull(out string a)
        {
            a = null;
        }

        public void ByRef_Out_Nullable_WithNonNull(out string a)
        {
            a = "value";
        }

        public void ByRef_Out_NonNullNullable_WithNull(out string a)
        {
            a = "newValue";
        }

        #endregion
        #region Number

        public void ByRef_Ref_InZero_OutZero(ref int a)
        {
            a = 0;
        }

        public void ByRef_Ref_InZero_OutValue(ref int a)
        {
            a = 1;
        }

        public void ByRef_Ref_InNonZero_OutValue(ref int a)
        {
            a = 2;
        }

        public void ByRef_Out_Number_WithZero(out int a)
        {
            a = 0;
        }

        public void ByRef_Out_Number_WithNonZero(out int a)
        {
            a = 1;
        }

        public void ByRef_Out_NonZeroNumber_WithNonZero(out int a)
        {
            a = 2;
        }

        #endregion
        #region Ptr

        public unsafe void ByRef_Ref_InPtrZero_OutZero(ref int* a)
        {
            a = (int*) 0;
        }

        public unsafe void ByRef_Ref_InPtrZero_OutValue(ref int* a)
        {
            a = (int*) 1;
        }

        public unsafe void ByRef_Ref_InPtrNonZero_OutValue(ref int* a)
        {
            a = (int*) 2;
        }

        public unsafe void ByRef_Out_Ptr_WithZero(out int* a)
        {
            a = (int*) 0;
        }

        public unsafe void ByRef_Out_Ptr_NonWithZero(out int* a)
        {
            a = (int*) 1;
        }

        public unsafe void ByRef_Out_NonZeroPtr_NonWithZero(out int* a)
        {
            a = (int*) 2;
        }

        #endregion
        #endregion

        public unsafe void FnPtrArg(delegate*<void> a)
        {
            Debug.WriteLine(((IntPtr)a).ToInt64().ToString("X"));
        }

        public unsafe void FnPtrNullArg(delegate*<void> a)
        {
            Debug.WriteLine(((IntPtr)a).ToInt64().ToString("X"));
        }

        public void DecimalArg(decimal a)
        {
        }

        public void StringArg(string a)
        {
        }

        public void ObjectArg(object a)
        {
        }

        public void InterfaceArg(IInterface a)
        {
        }

        public void NestedNestedExternalType(Dictionary<string, int>.ValueCollection.Enumerator a)
        {
        }

        public void GenericWithObjectTypeArg(GenericValueTypeType<object> a)
        {
        }

        public void GenericClassArg(GenericClassType<Class1WithField> a)
        {
        }

        public void GenericValueTypeArg(GenericValueTypeType<Struct1WithField> a)
        {
        }

        public void GenericTwoTypeArgs(Dictionary<string, int> a)
        {
        }

        public void GenericClass_ToObjectArg(object a)
        {
        }

        public void GenericValueType_ToObjectArg(object a)
        {
        }

        public void Generic_MethodVar_ElementTypeClassArg<TMethod>(TMethod a)
        {
        }

        public void Generic_MethodVar_ElementTypeValueTypeArg<TMethod>(TMethod a)
        {
        }

        public void Generic_MethodVar_ElementTypeSimpleArg<TMethod>(TMethod a)
        {
        }

        public void Generic_MethodVar_ElementTypeClassArrayArg<TMethod>(TMethod[] a)
        {
        }

        public void Generic_MethodVar_ElementTypeValueTypeArrayArg<TMethod>(TMethod[] a)
        {
        }

        public void Generic_MethodVar_ElementTypeSimpleArrayArg<TMethod>(TMethod[] a)
        {
        }

        public void Generic_MethodVar_ElementTypeGenericClassArg<TMethod>(GenericClassType<TMethod> a)
        {
        }

        public void Generic_MethodVar_ElementTypeGenericClassArrayArg<TMethod>(GenericClassType<TMethod> a)
        {
        }

        public void Generic_MethodVar_ElementTypeGenericValueTypeArg<TMethod>(GenericValueTypeType<TMethod> a)
        {
        }

        public void Generic_MethodVar_ElementTypeGenericValueTypeArrayArg<TMethod>(GenericValueTypeType<TMethod> a)
        {
        }

        public void Generic_MethodVar_ElementTypeGenericValueType_SimpleArg<TMethod>(GenericValueTypeType<TMethod> a)
        {
        }

        public void Generic_MethodVar_ElementTypeNullablePrimitive(int? a)
        {
        }

        public void Generic_MethodVar_ElementTypeNullableValueType(DateTime? a)
        {
        }

        public void Generic_MethodVar_ElementTypeGenericValueType_SZArrayValueArg<TMethod>(GenericValueTypeType<TMethod>[] a)
        {
        }

        public void Generic_MethodVar_ElementTypeGenericValueType_SZArrayArg<TMethod>(GenericValueTypeType<TMethod[]> a)
        {
        }

        public void Generic_MethodVar_ElementTypeGenericValueType_MultiArrayValueArg<TMethod>(GenericValueTypeType<TMethod>[,] a)
        {
        }

        public void Generic_MethodVar_ElementTypeGenericValueType_MultiArrayArg<TMethod>(GenericValueTypeType<TMethod[,]> a)
        {
        }

        public void EmptyStringArg(string a)
        {
        }

        public void NullStringArg(string a)
        {
        }

        public void StringArrayArg(string[] a)
        {
        }

        public void EmptyStringArrayArg(string[] a)
        {
        }

        public void ObjectArrayContainingStringArg(object[] a)
        {
        }

        public void MultiArrayThreeDimensionsArg(int[,,] a)
        {
        }

        public void ClassArg(Class1 a)
        {
        }

        public void ClassWithFieldArg(Class1WithField a)
        {
        }

        public void ClassWithPropertyArg(Class1WithProperty a)
        {
        }

        public void ExternalClass(Uri a)
        {
        }

        public void ClassArrayArg(Class1[] a)
        {
        }

        public void EmptyClassArrayArg(Class1[] a)
        {
        }

        public void ObjectArrayArg(object[] a)
        {
        }

        public void EmptyObjectArrayArg(object[] a)
        {
        }

        public void ObjectArrayOfObjectArray(object[] a)
        {
        }

        public void ValueTypeArrayArg(int[] a)
        {
        }

        public void EmptyValueTypeArrayArg(int[] a)
        {
        }

        public void StructArg(Struct1 a)
        {
        }

        public void StructWithFieldArg(Struct1WithField a)
        {
        }

        public void StructWithPropertyArg(Struct1WithProperty a)
        {
        }

        public void ExternalStruct(DateTime a)
        {
        }

        public void StructArrayArg(Struct1WithProperty[] a)
        {
        }

        public void ExternalStructArrayArg(DateTime[] a)
        {
        }

        public void BoxedStructArrayArg(object[] a)
        {
        }

        public void BoxedExternalStructArrayArg(object[] a)
        {
        }

        public void VarArg1(string a, __arglist)
        {
            VarArg2("first", __arglist(2, true, "three"));
        }

        public void VarArg2(string a, __arglist)
        {
        }
    }
}
