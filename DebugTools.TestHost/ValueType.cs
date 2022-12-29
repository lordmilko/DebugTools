using System;

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

        public void DecimalArg(decimal a)
        {
        }

        public void StringArg(string a)
        {
        }

        public void ObjectArg(object a)
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

        public void Generic_MethodVar_ElementTypeGenericClassArg<TMethod>(GenericType<TMethod> a)
        {
        }

        public void Generic_MethodVar_ElementTypeGenericClassArrayArg<TMethod>(GenericType<TMethod> a)
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
