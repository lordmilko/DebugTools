using System;
using System.Runtime.InteropServices;

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

    [StructLayout(LayoutKind.Explicit)]
    public struct StructWithExplicitFields
    {
        [FieldOffset(0)]
        public short field1_0;

        [FieldOffset(2)]
        public short field1_2;

        [FieldOffset(0)]
        public int field2_0;
    }

    public struct StructWithExplicitBetweenStringAndInt
    {
        public string stringField;
        public StructWithExplicitFields explicitField;
        public int intField;

        public StructWithExplicitBetweenStringAndInt(string stringValue, StructWithExplicitFields explicitValue, int intValue)
        {
            stringField = stringValue;
            explicitField = explicitValue;
            intField = intValue;
        }
    }

    namespace Duplicate
    {
        struct DuplicateStructType
        {
        }
    }
}
