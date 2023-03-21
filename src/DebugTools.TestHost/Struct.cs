using System;

namespace DebugTools.TestHost
{
    struct Struct1
    {
    }

    struct StructWithPrimitiveField
    {
        public int field1;
    }

    struct StructWithReferenceField
    {
        public string field1;
    }

    struct StructWithFieldWithPrimitiveField
    {
        public StructWithPrimitiveField field1;
    }

    struct StructWithPrimitiveProperty
    {
        public int Property1 { get; set; }
    }

    struct StructWithReferenceProperty
    {
        public string Property1 { get; set; }
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
        public StructWithPrimitiveField Struct;
        public StructWithPrimitiveField* StructPtr;
    }

    namespace Duplicate
    {
        struct DuplicateStructType
        {
        }
    }
}
