using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Profiler.Tests
{
    class DummyClass_Member
    {
#pragma warning disable CS0649
        public string StringField;
        public string StringProperty { get; set; }
#pragma warning restore CS0169
    }

    [TestClass]
    public class DynamicMemberTests : BaseDynamicTest
    {
        [TestMethod]
        public void Dynamic_Member_Field()
        {
            Test<DummyClass_Member>(value =>
            {
                Assert.IsNull(value.StringField);

                value.StringField = "foo";

                Assert.AreEqual("foo", value.StringField);
            });
        }

        [TestMethod]
        public void Dynamic_Member_Property()
        {
            Test<DummyClass_Member>(value =>
            {
                Assert.IsNull(value.StringProperty);

                value.StringProperty = "foo";

                Assert.AreEqual("foo", value.StringProperty);
            });
        }

        [TestMethod]
        public void Dynamic_Member_PrimitiveValue()
        {
            Test<DummyClass_Index_PrimitiveKey_PrimitiveValue>(value =>
            {
                Assert.AreEqual(0, value.Value);

                value.Value = 2;

                Assert.AreEqual(2, value.Value);
            });
        }

        [TestMethod]
        public void Dynamic_Member_StringValue()
        {
            Test<DummyClass_Index_StringKey_StringValue>(value =>
            {
                Assert.IsNull(value.Value);

                value.Value = "foo";

                Assert.AreEqual("foo", value.Value);
            });
        }

        [TestMethod]
        public void Dynamic_Member_ComplexValue()
        {
            Test<DummyClass_Index_ComplexKey_ComplexValue, DummyClass_Index_PrimitiveKey_PrimitiveValue>(
                (outer, inner) =>
                {
                    Assert.IsNull(outer.Value);

                    outer.Value = inner;

                    Assert.AreEqual(inner, outer.Value);
                }
            );
        }

        [TestMethod]
        public void Dynamic_Member_StructValue()
        {
            Test<DummyClass_Index_StructKey_StructValue, DummyStruct>((outer, inner) =>
            {
                Assert.AreEqual("Profiler.Tests.DummyClass_Index_StructKey_StructValue", outer.ToString());
                Assert.AreEqual("Profiler.Tests.DummyStruct", inner.ToString());

                var existing1 = outer.Value;

                Assert.AreEqual(0, existing1.Value);

                inner.Value = 2;
                Assert.AreEqual(0, existing1.Value);

                var existing2 = outer.Value;
                Assert.AreEqual(0, existing2.Value);

                outer.Value = inner;
                var existing3 = outer.Value;

                Assert.AreEqual(2, existing3.Value);
            });
        }

        [TestMethod]
        public void Dynamic_Member_WellKnownStructValue()
        {
            Test<DummyClass_Index_WellKnownStructKey_WellKnownStructValue>(value =>
            {
                var now = DateTime.Now;

                DateTime initial = value.Value;
                Assert.AreEqual(0, initial.Ticks);

                value.Value = now;

                var updated = value.Value;

                Assert.AreEqual(now, updated);
            });
        }

        [TestMethod]
        public void Dynamic_Member_InvalidType_Throws()
        {
            Test<DummyClass_Index_PrimitiveKey_PrimitiveValue>(value =>
            {
                AssertEx.Throws<ArgumentException>(
                    () => value.Value = "test",
                    "Object of type 'System.String' cannot be converted to type 'System.Int32'."
                );
            });
        }
    }
}
