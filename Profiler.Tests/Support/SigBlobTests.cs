using System;
using ClrDebug;
using DebugTools;
using DebugTools.Profiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Profiler.Tests
{
    [TestClass]
    public class SigBlobTests : BaseTest
    {
        private static MetaDataImport import;
        private static mdTypeDef typeDef;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            var dispenser = new MetaDataDispenserEx();

            import = dispenser.OpenScope<MetaDataImport>(ProfilerInfo.TestHost, CorOpenFlags.ofReadOnly);
            typeDef = import.FindTypeDefByName("DebugTools.TestHost.SigBlobType", mdToken.Nil);
        }

        #region SigMethodDef

        [TestMethod]
        public void SigMethodDef_NoArgs_ReturnVoid() =>
            TestMethodDef(SigBlobTestType.NoArgs_ReturnVoid, v => v.HasNoParams().ReturnsVoid());

        [TestMethod]
        public void SigMethodDef_OneArg_ReturnVoid() =>
            TestMethodDef(SigBlobTestType.OneArg_ReturnVoid, v => v.HasParam<int>(0).ReturnsVoid());

        #region BOOLEAN | CHAR | I1 | U1 | I2 | U2 | I4 | U4 | I8 | U8 | R4 | R8 | I | U

        [TestMethod]
        public void SigMethodDef_BoolArg() =>
            TestMethodDef(SigBlobTestType.BoolArg, v => v.HasParam<bool>(0));

        [TestMethod]
        public void SigMethodDef_CharArg() =>
            TestMethodDef(SigBlobTestType.CharArg, v => v.HasParam<char>(0));

        [TestMethod]
        public void SigMethodDef_ByteArg() =>
            TestMethodDef(SigBlobTestType.ByteArg, v => v.HasParam<byte>(0));

        [TestMethod]
        public void SigMethodDef_SByteArg() =>
            TestMethodDef(SigBlobTestType.SByteArg, v => v.HasParam<sbyte>(0));

        [TestMethod]
        public void SigMethodDef_Int16Arg() =>
            TestMethodDef(SigBlobTestType.Int16Arg, v => v.HasParam<short>(0));

        [TestMethod]
        public void SigMethodDef_UInt16Arg() =>
            TestMethodDef(SigBlobTestType.UInt16Arg, v => v.HasParam<ushort>(0));

        [TestMethod]
        public void SigMethodDef_Int32Arg() =>
            TestMethodDef(SigBlobTestType.Int32Arg, v => v.HasParam<int>(0));

        [TestMethod]
        public void SigMethodDef_UInt32Arg() =>
            TestMethodDef(SigBlobTestType.UInt32Arg, v => v.HasParam<uint>(0));

        [TestMethod]
        public void SigMethodDef_Int64Arg() =>
            TestMethodDef(SigBlobTestType.Int64Arg, v => v.HasParam<long>(0));

        [TestMethod]
        public void SigMethodDef_UInt64Arg() =>
            TestMethodDef(SigBlobTestType.UInt64Arg, v => v.HasParam<ulong>(0));

        [TestMethod]
        public void SigMethodDef_FloatArg() =>
            TestMethodDef(SigBlobTestType.FloatArg, v => v.HasParam<float>(0));

        [TestMethod]
        public void SigMethodDef_DoubleArg() =>
            TestMethodDef(SigBlobTestType.DoubleArg, v => v.HasParam<double>(0));

        [TestMethod]
        public void SigMethodDef_IntPtrArg() =>
            TestMethodDef(SigBlobTestType.IntPtrArg, v => v.HasParam<IntPtr>(0));

        [TestMethod]
        public void SigMethodDef_UIntPtrArg() =>
            TestMethodDef(SigBlobTestType.UIntPtrArg, v => v.HasParam<UIntPtr>(0));

        #endregion
        #endregion
        #region SigMethodRef

        [TestMethod]
        public void SigMethodRef_VarArgs()
        {
            var methodDef = import.FindMethod(typeDef, "VarArg2", IntPtr.Zero, 0);
            var memberRef = import.FindMemberRef(methodDef, "VarArg2", IntPtr.Zero, 0);

            var memberDefProps = import.GetMethodProps(methodDef);
            var memberRefProps = import.GetMemberRefProps(memberRef);

            var defReader = new SigReader(memberDefProps.ppvSigBlob, memberDefProps.pcbSigBlob, methodDef, import);
            var refReader = new SigReader(memberRefProps.ppvSigBlob, memberRefProps.pbSig, memberRef, import);

            var sigMethodDef = defReader.ParseMethod("VarArg2", true);
            var sigMethodRef = refReader.ParseMethod("VarArg2", true);

            Assert.AreEqual("void VarArg2(string a, __arglist)", sigMethodDef.ToString());
            Assert.AreEqual("void VarArg2(string, int, bool, string)", sigMethodRef.ToString());
        }

        #endregion

        internal void TestMethodDef(SigBlobTestType type, Action<SigMethodVerifier> validate)
        {
            var methodDef = import.FindMethod(typeDef, type.ToString(), IntPtr.Zero, 0);

            var methodProps = import.GetMethodProps(methodDef);

            var reader = new SigReader(methodProps.ppvSigBlob, methodProps.pcbSigBlob, methodDef, import);
            var sigMethod = reader.ParseMethod(methodProps.szMethod, true);

            var verifier = sigMethod.Verify();

            validate(verifier);
        }
    }
}
