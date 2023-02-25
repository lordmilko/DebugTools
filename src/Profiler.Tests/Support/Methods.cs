using System;

namespace Profiler.Tests
{
    interface IIface
    {
    }

#pragma warning disable 0649 //Field is never assigned to, and will always have its default value
    class TestClass : IDisposable, IIface
    {
        public string Field1;

        public void Dispose()
        {
        }
    }

    class TestClassWithClassField
    {
        public TestClass Field1;
    }

    class TestClassWithSZArrayField
    {
        public string[] Field1;
    }

    class TestClassWithSZArrayInterfaceField : IIface
    {
        public IIface[] Field1;
    }

    class TestClassWithArrayInterfaceField : IIface
    {
        public IIface[,] Field1;
    }

    class TestClassWithArrayField
    {
        public string[,] Field1;
    }

    unsafe class TestClassWithPointerSimpleField : IIface
    {
        public char* Field1;
    }

    class TestClassWithInterfaceField : IIface
    {
        public IIface Field2;
    }

    struct TestStructWithSimpleField : IDisposable, IIface
    {
        public int Field2;

        public void Dispose()
        {
        }
    }

    unsafe struct TestStructWithPointerField
    {
        public char* Field1;
    }

    struct TestStructWithInterfaceField : IIface
    {
        public IIface Field1;
    }

    unsafe class TestClassWithPointerStructField
    {
        public TestStructWithSimpleField* Field1;
    }

    unsafe class TestClassWithPointerPointerSimpleField
    {
        public char** Field1;
    }

    class TestGenericClass<T>
    {
        public T Field1;
    }

#pragma warning restore 0649 //Field is never assigned to, and will always have its default value

    internal static class Methods
    {
        public static void StringArg(string a)
        {
        }

        public static void ClassArg(TestClass a)
        {
        }

        public static void ClassWithClassFieldArg(TestClassWithClassField a)
        {
        }

        public static void SZArrayArg(string[] a)
        {
        }

        public static void SZArrayField(TestClassWithSZArrayField a)
        {
        }

        public static void ArrayArg(object[,] a)
        {
        }

        public static void ArrayField(TestClassWithArrayField a)
        {
        }

        public static void GenericArg<T>(T a)
        {
        }

        public static void ForeignInterfaceArg(IDisposable a)
        {
        }

        public static void LocalInterfaceArg(IIface a)
        {
        }

        public static unsafe void PointerSimpleArg(char* a)
        {
        }

        public static unsafe void PointerStructArg(TestStructWithSimpleField* a)
        {
        }

        public static void PointerSimpleField(TestClassWithPointerSimpleField a)
        {
        }

        public static void PointerPointerSimpleField(TestClassWithPointerPointerSimpleField a)
        {
        }

        public static void PointerStructField(TestClassWithPointerStructField a)
        {
        }

        public static void first(object a)
        {
        }

        public static void second(object a)
        {
        }

        public static void third(object a)
        {
        }
    }
}
