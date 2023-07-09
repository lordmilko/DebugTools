using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Profiler.Tests
{
    struct DynamicClass_InvokeMember : IIface
    {
        public int PrimitiveValue(int value) => value * 2;

        public string StringValue(string value) => $"{value} world";

        public string[] ParamsValue(params string[] values)
        {
            var vals = values.ToList();
            vals.Add("world");
            return vals.ToArray();
        }

        //Dummy overload
        public string[] ParamsValue(int a, int b, params string[] values)
        {
            throw new NotImplementedException();
        }

        public int DefaultValue(int a = 1, int b = 2) => a * b;

        //Dummy overload
        public string DefaultValue(string value)
        {
            throw new NotImplementedException();
        }
    }

    [TestClass]
    public class DynamicInvokeMemberTests : BaseDynamicTest
    {
        [TestMethod]
        public void Dynamic_InvokeMember_PrimitiveValue()
        {
            Test<DynamicClass_InvokeMember>(value =>
            {
                var result = value.PrimitiveValue(2);

                Assert.AreEqual(4, result);
            });
        }

        [TestMethod]
        public void Dynamic_InvokeMember_StringValue()
        {
            Test<DynamicClass_InvokeMember>(value =>
            {
                var result = value.StringValue("hello");

                Assert.AreEqual("hello world", result);
            });
        }

        [TestMethod]
        public void Dynamic_InvokeMember_MissingParameters_Throws()
        {
            Test<DynamicClass_InvokeMember>(value =>
            {
                AssertEx.Throws<ArgumentException>(
                    () => value.StringValue(),
                    "Expected a value for parameter 'System.String value' however none was specified."
                );
            });
        }

        [TestMethod]
        public void Dynamic_InvokeMember_ParamsArray_NoArgs()
        {
            Test<DynamicClass_InvokeMember>(value =>
            {
                var result = (string[]) value.ParamsValue();

                Assert.AreEqual(1, result.Length);
                Assert.AreEqual("world", result[0]);
            });
        }

        [TestMethod]
        public void Dynamic_InvokeMember_ParamsArray_OneArg()
        {
            Test<DynamicClass_InvokeMember>(value =>
            {
                var result = (string[])value.ParamsValue("hello");

                Assert.AreEqual(2, result.Length);
                Assert.AreEqual("hello", result[0]);
                Assert.AreEqual("world", result[1]);
            });
        }

        [TestMethod]
        public void Dynamic_InvokeMember_ParamsArray_TwoArgs()
        {
            Test<DynamicClass_InvokeMember>(value =>
            {
                var result = (string[])value.ParamsValue("hello", "there");

                Assert.AreEqual(3, result.Length);
                Assert.AreEqual("hello", result[0]);
                Assert.AreEqual("there", result[1]);
                Assert.AreEqual("world", result[2]);
            });
        }

        [TestMethod]
        public void Dynamic_InvokeMember_DefaultParameters_NoneSpecified()
        {
            Test<DynamicClass_InvokeMember>(value =>
            {
                var result = value.DefaultValue();

                Assert.AreEqual(2, result);
            });
        }

        [TestMethod]
        public void Dynamic_InvokeMember_DefaultParameters_OneSpecified()
        {
            Test<DynamicClass_InvokeMember>(value =>
            {
                var result = value.DefaultValue(2);

                Assert.AreEqual(4, result);
            });
        }

        [TestMethod]
        public void Dynamic_InvokeMember_DefaultParameters_AllSpecified()
        {
            Test<DynamicClass_InvokeMember>(value =>
            {
                var result = value.DefaultValue(2, 4);

                Assert.AreEqual(8, result);
            });
        }
    }
}
