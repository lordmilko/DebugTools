namespace Profiler.Tests
{
#pragma warning disable 0649 //Field is never assigned to, and will always have its default value
    class TestClass
    {
        public string Field1;
    }

    class TestClassWithClassField
    {
        public TestClass Field1;
    }

    class TestClassWithSZArrayField
    {
        public string[] Field1;
    }

    class TestClassWithArrayField
    {
        public string[,] Field1;
    }

    unsafe class TestClassWithPointerSimpleField
    {
        public char* Field1;
    }

    struct TestStructWithSimpleField
    {
        public int Field2;
    }

    unsafe struct TestStructWithPointerField
    {
        public char* Field1;
    }

    unsafe class TestClassWithPointerStructField
    {
        public TestStructWithSimpleField* Field1;
    }

    unsafe class TestClassWithPointerPointerSimpleField
    {
        public char** Field1;
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
