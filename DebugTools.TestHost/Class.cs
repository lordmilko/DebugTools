namespace DebugTools.TestHost
{
    class Class1
    {
    }

    class Class1WithField
    {
        public int field1;
    }

    class Class1WithProperty
    {
        public int Property1 { get; set; }
    }

    interface IInterface
    {
    }

    class ImplInterface : IInterface
    {
    }

    class GenericClassType<T>
    {
        public T field1;
    }

    struct GenericValueTypeType<T>
    {
        public T field1;
    }
}
