using System;
using System.Linq;
using ClrDebug;
using DebugTools.Profiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
            Test(ValueTestType.StructArg, v => v.HasArrayValues(CorElementType.I4, 1, 2));

        [TestMethod]
        public void Value_StructWithFieldArg() =>
            Test(ValueTestType.StructWithFieldArg, v => v.HasFieldValue(1));

        [TestMethod]
        public void Value_StructWithPropertyArg() =>
            Test(ValueTestType.StructWithPropertyArg, v => v.HasFieldValue(1));

        #endregion

        internal void Test(ValueTestType type, Action<FrameVerifier> validate, params ProfilerEnvFlags[] flags)
        {
            var flagsList = flags.ToList();
            flagsList.Add(ProfilerEnvFlags.Detailed);

            TestInternal(TestType.Value, type.ToString(), v => validate(v.FindFrame(type.ToString()).Verify()), flagsList.ToArray());
        }
    }
}
