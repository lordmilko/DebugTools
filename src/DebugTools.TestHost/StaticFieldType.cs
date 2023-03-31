using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace DebugTools.TestHost
{
    class StaticFieldType
    {
        static StaticFieldType()
        {
            //In Release builds, it seems that the static fields may not actually get initialized if they aren't actually accessed.
            //As such, we access one of the fields
            Console.WriteLine(primitiveType);
        }

#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable IDE0052 // Remove unread private members
#pragma warning disable CS0414
        private static int primitiveType = 3;
        private static string stringType = "foo";
        private static string StringPropertyType { get; } = "bar";
        private static DuplicateStructType duplicateStructType1 = new DuplicateStructType();
        private static Duplicate.DuplicateStructType duplicateStructType2 = new Duplicate.DuplicateStructType();

        private static Class1WithFieldWithField complexClassType = new Class1WithFieldWithField
        {
            field1 = new Class1WithField
            {
                field1 = 4
            }
        };

        private static StructWithFieldWithPrimitiveField complexStructType = new StructWithFieldWithPrimitiveField
        {
            field1 = new StructWithPrimitiveField
            {
                field1 = 5
            }
        };

        private static GenericStructType<int> complexGenericStructType = new GenericStructType<int>
        {
            field1 = 6
        };

        [ThreadStatic]
        private static int threadStaticPrimitiveType = 8;

#pragma warning restore CS0414
#pragma warning restore IDE0044 // Add readonly modifier
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore IDE0052 // Remove unread private members

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Normal()
        {
            Sleep();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Sleep()
        {
            while (true)
                Thread.Sleep(1);
        }
    }
}
