using System;

namespace DebugTools.TestHost
{
    struct Struct1
    {
    }

    struct Struct1WithField
    {
        public int field1;
    }

    struct Struct1WithFieldWithField
    {
        public Struct1WithField field1;
    }

    struct Struct1WithProperty
    {
        public int Property1 { get; set; }
    }

    struct GenericStructType<T>
    {
        private static T staticValue;

        static GenericStructType()
        {
            if (typeof(T) == typeof(int))
                staticValue = (T) (object) 7;
            else
                throw new NotImplementedException($"Don't know what default value to assign to {nameof(staticValue)}");
        }

        public T field1;
    }

    struct DuplicateStructType
    {
    }

    unsafe struct ComplexPtrStruct
    {
        public char* CharPtr;
        public char CharVal;
        public Struct1WithField Struct;
        public Struct1WithField* StructPtr;
    }

    namespace Duplicate
    {
        struct DuplicateStructType
        {
        }
    }
}
