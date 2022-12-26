using System;
using System.Linq;
using ClrDebug;
using DebugTools.Profiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ValueType = DebugTools.Profiler.ValueType;

namespace Profiler.Tests
{
    [TestClass]
    public class ValueTests : BaseTest
    {
        #region BOOLEAN | CHAR | I1 | U1 | I2 | U2 | I4 | U4 | I8 | U8 | R4 | R8 | I | U

        [TestMethod]
        public void Value_BoolArg() =>
            Test(ValueTestType.BoolArg, v => v.HasValue(true));

        [TestMethod]
        public void Value_CharArg() =>
            Test(ValueTestType.CharArg, v => v.HasValue('b'));

        [TestMethod]
        public void Value_SByteArg() =>
            Test(ValueTestType.SByteArg, v => v.HasValue<sbyte>(1));

        [TestMethod]
        public void Value_ByteArg() =>
            Test(ValueTestType.ByteArg, v => v.HasValue<byte>(1));

        [TestMethod]
        public void Value_Int16Arg() =>
            Test(ValueTestType.Int16Arg, v => v.HasValue<short>(1));

        [TestMethod]
        public void Value_UInt16Arg() =>
            Test(ValueTestType.UInt16Arg, v => v.HasValue<ushort>(1));

        [TestMethod]
        public void Value_Int32Arg() =>
            Test(ValueTestType.Int32Arg, v => v.HasValue<int>(1));

        [TestMethod]
        public void Value_UInt32Arg() =>
            Test(ValueTestType.UInt32Arg, v => v.HasValue<uint>(1));

        [TestMethod]
        public void Value_Int64Arg() =>
            Test(ValueTestType.Int64Arg, v => v.HasValue<long>(1));

        [TestMethod]
        public void Value_UInt64Arg() =>
            Test(ValueTestType.UInt64Arg, v => v.HasValue<ulong>(1));

        [TestMethod]
        public void Value_FloatArg() =>
            Test(ValueTestType.FloatArg, v => v.HasValue<float>(1.1f));

        [TestMethod]
        public void Value_DoubleArg() =>
            Test(ValueTestType.DoubleArg, v => v.HasValue<double>(1.1));

        [TestMethod]
        public void Value_IntPtrArg() =>
            Test(ValueTestType.IntPtrArg, v => v.HasValue(new IntPtr(1)));

        [TestMethod]
        public void Value_UIntPtrArg() =>
            Test(ValueTestType.UIntPtrArg, v => v.HasValue(new UIntPtr(1)));

        #endregion
        #region String

        [TestMethod]
        public void Value_StringArg() =>
            Test(ValueTestType.StringArg, v => v.HasValue("foo"));

        [TestMethod]
        public void Value_EmptyStringArg() =>
            Test(ValueTestType.EmptyStringArg, v => v.HasValue(string.Empty));

        [TestMethod]
        public void Value_NullStringArg() =>
            Test(ValueTestType.NullStringArg, v => v.HasValue<string>(null));

        #endregion

        [TestMethod]
        public void Value_ObjectArg() =>
            Test(ValueTestType.ObjectArg, v => v.HasClassType("System.Object"), ProfilerEnvFlags.WaitForDebugger);

        #region String Array

        [TestMethod]
        public void Value_StringArrayArg() =>
            Test(ValueTestType.StringArrayArg, v => v.HasArrayValues(CorElementType.Class, "a", "b"));

        [TestMethod]
        public void Value_EmptyStringArrayArg() =>
            Test(ValueTestType.EmptyStringArrayArg, v => v.HasArrayValues(CorElementType.Class, new string[0]));

        [TestMethod]
        public void Value_ObjectArrayContainingStringArg() =>
            Test(ValueTestType.ObjectArrayContainingStringArg, v => v.HasArrayValues(CorElementType.Class, "a"));

        #endregion
        #region Class

        [TestMethod]
        public void Value_ClassArg() =>
            Test(ValueTestType.ClassArg, v => v.HasClassType("DebugTools.TestHost.Class1"));

        [TestMethod]
        public void Value_ClassWithFieldArg() =>
            Test(ValueTestType.ClassWithFieldArg, v => v.HasFieldValue(1));

        [TestMethod]
        public void Value_ClassWithPropertyArg() =>
            Test(ValueTestType.ClassWithPropertyArg, v => v.HasFieldValue(1));

        [TestMethod]
        public void Value_ExternalClass() =>
            Test(ValueTestType.ExternalClass, v => v.HasFieldValue(0, "https://www.google.com"), ProfilerEnvFlags.WaitForDebugger);

        #endregion
        #region Class Array

        [TestMethod]
        public void Value_ClassArrayArg() =>
            Test(ValueTestType.ClassArrayArg, v =>
                v.HasArrayClassValues(CorElementType.Class, "DebugTools.TestHost.Class1", "DebugTools.TestHost.Class1"));

        [TestMethod]
        public void Value_EmptyClassArrayArg() =>
            Test(ValueTestType.EmptyClassArrayArg, v => v.HasArrayClassValues(CorElementType.Class));

        #endregion
        #region Object Array

        [TestMethod]
        public void Value_ObjectArrayArg() =>
            Test(ValueTestType.ObjectArrayArg, v => v.HasArrayClassValues(CorElementType.Class, "System.Object"));

        [TestMethod]
        public void Value_EmptyObjectArrayArg() =>
            Test(ValueTestType.EmptyObjectArrayArg, v => v.HasArrayValues(CorElementType.Class, new object[0]));

        [TestMethod]
        public void Value_ObjectArrayOfObjectArray() =>
            Test(ValueTestType.ObjectArrayOfObjectArray, v =>
            {
                var parameter = (SZArrayValue) v.GetParameter();

                Assert.AreEqual(2, parameter.Value.Length);

                var firstArray = (SZArrayValue) parameter.Value[0];
                var secondArray = (SZArrayValue) parameter.Value[1];

                Assert.AreEqual(1, firstArray.Value.Length);
                Assert.AreEqual(1, secondArray.Value.Length);

                var firstValue = (Int32Value) firstArray.Value[0];
                var secondValue = (StringValue) secondArray.Value[0];

                Assert.AreEqual(1, firstValue.Value);
                Assert.AreEqual("2", secondValue.Value);
            });

        #endregion
        #region ValueType Array

        [TestMethod]
        public void Value_ValueTypeArrayArg() =>
            Test(ValueTestType.ValueTypeArrayArg, v => v.HasArrayValues(CorElementType.I4, 1, 2));

        [TestMethod]
        public void Value_EmptyValueTypeArrayArg() =>
            Test(ValueTestType.EmptyValueTypeArrayArg, v => v.HasArrayValues(CorElementType.I4, new int[0]));

        #endregion
        #region Struct

        [TestMethod]
        public void Value_StructArg() =>
            Test(ValueTestType.StructArg, v => v.HasValueType("DebugTools.TestHost.Struct1"));

        [TestMethod]
        public void Value_StructWithFieldArg() =>
            Test(ValueTestType.StructWithFieldArg, v => v.HasFieldValue(1));

        [TestMethod]
        public void Value_StructWithPropertyArg() =>
            Test(ValueTestType.StructWithPropertyArg, v => v.HasFieldValue(1));

        [TestMethod]
        public void Value_ExternalStruct() =>
            Test(ValueTestType.ExternalStruct, v => v.HasFieldValue((ulong) 637949198450000000));

        #endregion
        #region Struct Array

        [TestMethod]
        public void Value_StructArrayArg() =>
            Test(ValueTestType.StructArrayArg, v =>
            {
                v.HasArrayStructValues(CorElementType.ValueType, "DebugTools.TestHost.Struct1WithProperty", "DebugTools.TestHost.Struct1WithProperty");

                var parameter = v.GetParameter();

                var arrObj = (SZArrayValue)parameter;
                var firstElm = (ValueType)arrObj.Value[0];
                var secondElm = (ValueType)arrObj.Value[1];

                var firstValue = (Int32Value) firstElm.FieldValues[0];
                var secondValue = (Int32Value) secondElm.FieldValues[0];

                Assert.AreEqual(1, firstValue.Value);
                Assert.AreEqual(2, secondValue.Value);
            });

        [TestMethod]
        public void Value_ExternalStructArrayArg() =>
            Test(ValueTestType.ExternalStructArrayArg, v =>
            {
                v.HasArrayStructValues(CorElementType.ValueType, "System.DateTime", "System.DateTime");

                var parameter = v.GetParameter();

                var arrObj = (SZArrayValue)parameter;
                var firstElm = (ValueType)arrObj.Value[0];
                var secondElm = (ValueType)arrObj.Value[1];

                var firstValue = (UInt64Value) firstElm.FieldValues[0];
                var secondValue = (UInt64Value) secondElm.FieldValues[0];

                Assert.AreEqual((ulong) 637949198450000000, firstValue.Value);
                Assert.AreEqual((ulong)631006958450000000, secondValue.Value);
            });

        [TestMethod]
        public void Value_BoxedStructArrayArg() =>
            Test(ValueTestType.BoxedStructArrayArg, v =>
            {
                var parameter = (SZArrayValue) v.GetParameter();

                Assert.AreEqual(2, parameter.Value.Length);

                var first = (ValueType) parameter.Value[0];
                var second = (StringValue) parameter.Value[1];

                Assert.AreEqual(1, ((Int32Value) first.FieldValues[0]).Value);
                Assert.AreEqual("b", second.Value);
            });

        [TestMethod]
        public void Value_BoxedExternalStructArrayArg() =>
            Test(ValueTestType.BoxedExternalStructArrayArg, v =>
            {
                var parameter = (SZArrayValue)v.GetParameter();

                Assert.AreEqual(2, parameter.Value.Length);

                var first = (ValueType)parameter.Value[0];
                var second = (StringValue)parameter.Value[1];

                Assert.AreEqual((ulong) 637949198450000000, ((UInt64Value) first.FieldValues[0]).Value);
                Assert.AreEqual("b", second.Value);
            });

        #endregion

        internal void Test(ValueTestType type, Action<FrameVerifier> validate, params ProfilerEnvFlags[] flags)
        {
            var flagsList = flags.ToList();
            flagsList.Add(ProfilerEnvFlags.Detailed);

            TestInternal(TestType.Value, type.ToString(), v => validate(v.FindFrame(type.ToString()).Verify()), flagsList.ToArray());
        }
    }
}
