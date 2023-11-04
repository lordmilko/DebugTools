using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CSharp.RuntimeBinder;

namespace Profiler.Tests
{
    class DummyClass_Index_PrimitiveKey_PrimitiveValue
    {
        public int Value { get; set; }

        public int this[int index]
        {
            get => Value;
            set => Value = value;
        }
    }

    class DummyClass_Index_StringKey_StringValue
    {
        public string Value { get; set; }

        public string this[string index]
        {
            get => Value;
            set => Value = value;
        }
    }

    class DummyClass_Index_ComplexKey_ComplexValue
    {
        public DummyClass_Index_PrimitiveKey_PrimitiveValue Value { get; set; }

        public DummyClass_Index_PrimitiveKey_PrimitiveValue this[DummyClass_Index_PrimitiveKey_PrimitiveValue index]
        {
            get => Value;
            set => Value = value;
        }
    }

    struct DummyStruct
    {
        public int Value { get; set; }
    }

    class DummyClass_Index_StructKey_StructValue
    {
        public DummyStruct Value { get; set; }

        public DummyStruct this[DummyStruct index]
        {
            get => Value;
            set => Value = value;
        }
    }

    class DummyClass_Index_WellKnownStructKey_WellKnownStructValue
    {
        public DateTime Value { get; set; }

        public DateTime this[DateTime index]
        {
            get => Value;
            set => Value = value;
        }
    }

    [TestClass]
    public class DynamicIndexTests : BaseDynamicTest
    {
        [TestMethod]
        public void Dynamic_Index_PrimitiveKey_PrimitiveValue()
        {
            Test<DummyClass_Index_PrimitiveKey_PrimitiveValue>(value =>
            {
                Assert.AreEqual(0, value[0]);

                value[0] = 2;

                Assert.AreEqual(2, value[0]);
            });
        }

        [TestMethod]
        public void Dynamic_Index_StringKey_StringValue()
        {
            Test<DummyClass_Index_StringKey_StringValue>(value =>
            {
                Assert.IsNull(value["hello"]);

                value["hello"] = "world";

                Assert.AreEqual("world", value["hello"]);
            });
        }

        [TestMethod]
        public void Dynamic_Index_ComplexKey_ComplexValue()
        {
            Test<DummyClass_Index_ComplexKey_ComplexValue, DummyClass_Index_PrimitiveKey_PrimitiveValue>(
                (outer, inner) =>
                {
                    Assert.IsNull(outer[inner]);

                    outer[inner] = inner;

                    Assert.AreEqual(inner, outer[inner]);
                }
            );
        }

        [TestMethod]
        public void Dynamic_Index_StructKey_StructValue()
        {
            Test<DummyClass_Index_StructKey_StructValue, DummyStruct>((outer, inner) =>
            {
                Assert.AreEqual("Profiler.Tests.DummyClass_Index_StructKey_StructValue", outer.ToString());
                Assert.AreEqual("Profiler.Tests.DummyStruct", inner.ToString());

                var existing1 = outer[inner];

                Assert.AreEqual(0, existing1.Value);

                inner.Value = 2;
                Assert.AreEqual(0, existing1.Value);

                var existing2 = outer[inner];
                Assert.AreEqual(0, existing2.Value);

                outer[inner] = inner;
                var existing3 = outer[inner];

                Assert.AreEqual(2, existing3.Value);
            });
        }

        [TestMethod]
        public void Dynamic_Index_WellKnownStructKey_WellKnownStructValue()
        {
            Test<DummyClass_Index_WellKnownStructKey_WellKnownStructValue>(value =>
            {
                var now = DateTime.Now;

                DateTime initial = value[now];
                Assert.AreEqual(0, initial.Ticks);

                value[now] = now;

                var updated = value[now];

                Assert.AreEqual(now, updated);
            });
        }

        [TestMethod]
        public void Dynamic_Index_InvalidType_Throws()
        {
            Test<DummyClass_Index_PrimitiveKey_PrimitiveValue>(value =>
            {
                AssertEx.Throws<ArgumentException>(
                    () => _ = value["test"],
                    "Object of type 'System.String' cannot be converted to type 'System.Int32'."
                );
            });
        }

        [TestMethod]
        public void Dynamic_Index_Array()
        {
            Test<DynamicClass_Enumerator>(value =>
            {
                var array = value.GetArray();

                var first = array[0];
                Assert.AreEqual(1, first.Value);

                var second = array[1];
                Assert.AreEqual(2, second.Value);
            });
        }

        [TestMethod]
        public void Dynamic_Index_List()
        {
            Test<DynamicClass_Enumerator>(value =>
            {
                var array = value.GetList();

                var first = array[0];
                Assert.AreEqual(1, first.Value);

                var second = array[1];
                Assert.AreEqual(2, second.Value);
            });
        }
    }
}
