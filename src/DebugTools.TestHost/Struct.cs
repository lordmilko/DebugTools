namespace DebugTools.TestHost
{
    struct Struct1
    {
    }

    struct Struct1WithField
    {
        public int field1;
    }

    struct Struct1WithProperty
    {
        public int Property1 { get; set; }
    }

    unsafe struct ComplexPtrStruct
    {
        public char* CharPtr;
        public char CharVal;
        public Struct1WithField Struct;
        public Struct1WithField* StructPtr;
    }
}
