using System;
using DebugTools.Profiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Profiler.Tests
{
    [TestClass]
    public class SigBlobTests : BaseTest
    {
        [TestMethod]
        public void SigMethod_NoArgs_ReturnVoid() =>
            Test(SigBlobTestType.NoArgs_ReturnVoid, v => v.HasNoParams().ReturnsVoid());

        [TestMethod]
        public void SigMethod_OneArg_ReturnVoid() =>
            Test(SigBlobTestType.OneArg_ReturnVoid, v => v.HasParam<int>(0).ReturnsVoid());

        #region BOOLEAN | CHAR | I1 | U1 | I2 | U2 | I4 | U4 | I8 | U8 | R4 | R8 | I | U

        [TestMethod]
        public void SigMethod_BoolArg() =>
            Test(SigBlobTestType.BoolArg, v => v.HasParam<bool>(0));

        [TestMethod]
        public void SigMethod_CharArg() =>
            Test(SigBlobTestType.CharArg, v => v.HasParam<char>(0));

        [TestMethod]
        public void SigMethod_ByteArg() =>
            Test(SigBlobTestType.ByteArg, v => v.HasParam<byte>(0));

        [TestMethod]
        public void SigMethod_SByteArg() =>
            Test(SigBlobTestType.SByteArg, v => v.HasParam<sbyte>(0));

        [TestMethod]
        public void SigMethod_Int16Arg() =>
            Test(SigBlobTestType.Int16Arg, v => v.HasParam<short>(0));

        [TestMethod]
        public void SigMethod_UInt16Arg() =>
            Test(SigBlobTestType.UInt16Arg, v => v.HasParam<ushort>(0));

        [TestMethod]
        public void SigMethod_Int32Arg() =>
            Test(SigBlobTestType.Int32Arg, v => v.HasParam<int>(0));

        [TestMethod]
        public void SigMethod_UInt32Arg() =>
            Test(SigBlobTestType.UInt32Arg, v => v.HasParam<uint>(0));

        [TestMethod]
        public void SigMethod_Int64Arg() =>
            Test(SigBlobTestType.Int64Arg, v => v.HasParam<long>(0));

        [TestMethod]
        public void SigMethod_UInt64Arg() =>
            Test(SigBlobTestType.UInt64Arg, v => v.HasParam<ulong>(0));

        [TestMethod]
        public void SigMethod_FloatArg() =>
            Test(SigBlobTestType.FloatArg, v => v.HasParam<float>(0));

        [TestMethod]
        public void SigMethod_DoubleArg() =>
            Test(SigBlobTestType.DoubleArg, v => v.HasParam<double>(0));

        [TestMethod]
        public void SigMethod_IntPtrArg() =>
            Test(SigBlobTestType.IntPtrArg, v => v.HasParam<IntPtr>(0));

        [TestMethod]
        public void SigMethod_UIntPtrArg() =>
            Test(SigBlobTestType.UIntPtrArg, v => v.HasParam<UIntPtr>(0));

        #endregion

        internal void Test(SigBlobTestType type, Action<SigMethodVerifier> validate)
        {
            TestInternal(TestType.SigBlob, type.ToString(), v =>
            {
                var frame = v.FindFrame(type.ToString());
                var info = (MethodInfoDetailed) frame.MethodInfo;
                var sigMethod = info.SigMethod;
                var verifier = sigMethod.Verify();

                validate(verifier);
            }, ProfilerEnvFlags.Detailed);
        }
    }
}