namespace Profiler.Tests
{
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

        public static void first(object a)
        {
        }

        public static void second(object a)
        {
        }
    }
}
