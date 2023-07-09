using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DebugTools.Dynamic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Profiler.Tests
{
    class DynamicClass_Enumerator
    {
        public DummyClass_Index_PrimitiveKey_PrimitiveValue[] GetArray()
        {
            return new[]
            {
                new DummyClass_Index_PrimitiveKey_PrimitiveValue
                {
                    Value = 1
                },
                new DummyClass_Index_PrimitiveKey_PrimitiveValue
                {
                    Value = 2
                },
            };
        }

        public List<DummyClass_Index_PrimitiveKey_PrimitiveValue> GetList()
        {
            return new List<DummyClass_Index_PrimitiveKey_PrimitiveValue>
            {
                new DummyClass_Index_PrimitiveKey_PrimitiveValue
                {
                    Value = 1
                },
                new DummyClass_Index_PrimitiveKey_PrimitiveValue
                {
                    Value = 2
                },
            };
        }
    }

    internal class NonSerializableException : Exception
    {
        public DummyClass_Index_PrimitiveKey_PrimitiveValue Value { get; set; }

        public NonSerializableException()
        {
            Value = new DummyClass_Index_PrimitiveKey_PrimitiveValue
            {
                Value = 1
            };
        }
    }

    class DummyClass_Exception
    {
        public void ThrowSerializable()
        {
            throw new InvalidOperationException("Test");
        }

        public void ThrowNonSerializable()
        {
            throw new NonSerializableException();
        }
    }

    [TestClass]
    public class DynamicTests : BaseDynamicTest
    {
        #region Exception

        [TestMethod]
        public void Dynamic_Exception_Serializable()
        {
            Test<DummyClass_Exception>(value =>
            {
                AssertEx.Throws<InvalidOperationException>(
                    () => value.ThrowSerializable(),
                    "Test"
                );
            });
        }

        [TestMethod]
        public void Dynamic_Exception_NonSerializable()
        {
            Test<DummyClass_Exception>(value =>
            {
                AssertEx.Throws<RemoteException>(
                    () => value.ThrowNonSerializable(),
                    "Exception of type 'Profiler.Tests.NonSerializableException' was thrown."
                );
            });
        }

        #endregion
        #region Enumerators

        [TestMethod]
        public void Dynamic_Enumerator_Array()
        {
            Test<DynamicClass_Enumerator>(value =>
            {
                var array = value.GetArray();

                Assert.IsInstanceOfType(array, typeof(EnumerableLocalProxyStub));

                var localArray = ((IEnumerable) array).Cast<dynamic>().ToArray();

                Assert.AreEqual(2, localArray.Length);
                Assert.AreEqual(1, localArray[0].Value);
                Assert.AreEqual(2, localArray[1].Value);
            });
        }

        [TestMethod]
        public void Dynamic_Enumerator_List()
        {
            Test<DynamicClass_Enumerator>(value =>
            {
                var array = value.GetList();

                Assert.IsInstanceOfType(array, typeof(EnumerableLocalProxyStub));

                var localArray = ((IEnumerable)array).Cast<dynamic>().ToArray();

                Assert.AreEqual(2, localArray.Length);
                Assert.AreEqual(1, localArray[0].Value);
                Assert.AreEqual(2, localArray[1].Value);
            });
        }

        #endregion
    }
}
