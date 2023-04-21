using System;
using System.Linq;
using System.Threading;
using ClrDebug;
using DebugTools.Profiler;
using DebugTools.Tracing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Profiler.Tests
{
    [TestClass]
    public class StaticFieldTests : BaseTest
    {
        [TestMethod]
        public void StaticField_PrimitiveType()
        {
            Test(s =>
            {
                var response = (IValue<int>) s.GetStaticField("StaticFieldType.primitiveType");

                Assert.AreEqual(3, response.Value);
            }, ProfilerSetting.Detailed);
        }

        [TestMethod]
        public void StaticField_StringType()
        {
            Test(s =>
            {
                var response = (IValue<string>)s.GetStaticField("StaticFieldType.stringType");

                Assert.AreEqual("foo", response.Value);
            }, ProfilerSetting.Detailed);
        }

        [TestMethod]
        public void StaticField_IgnoreCase()
        {
            Test(s =>
            {
                var response = (IValue<string>)s.GetStaticField("staticfieldtype.stringtype");

                Assert.AreEqual("foo", response.Value);
            }, ProfilerSetting.Detailed);
        }

        [TestMethod]
        public void StaticField_TypeAndNamespace()
        {
            Test(s =>
            {
                var response = (IValue<int>)s.GetStaticField("DebugTools.TestHost.StaticFieldType.primitiveType");

                Assert.AreEqual(3, response.Value);
            }, ProfilerSetting.Detailed);
        }

        [TestMethod]
        public void StaticField_TypeAndNamespaceIgnoreCase()
        {
            Test(s =>
            {
                var response = (IValue<int>)s.GetStaticField("debugtools.testhost.staticfieldtype.primitivetype");

                Assert.AreEqual(3, response.Value);
            }, ProfilerSetting.Detailed);
        }

        [TestMethod]
        public void StaticField_ComplexClassType()
        {
            Test(s =>
            {
                AssertEx.Throws<DebugException>(
                    () => s.GetStaticField("StaticFieldType.complexClassType"),
                    "Error HRESULT CORPROF_E_NOT_MANAGED_THREAD has been returned from a call to a COM component."
                );
            }, ProfilerSetting.Detailed);
        }

        [TestMethod]
        public void StaticField_ComplexStructType()
        {
            Test(s =>
            {
                var verifier = new ValueVerifier(s.GetStaticField("StaticFieldType.complexStructType"));

                verifier.HasValueType("DebugTools.TestHost.StructWithFieldWithPrimitiveField").HasFieldValue(MaxTraceDepth.Instance);
            }, ProfilerSetting.Detailed, ProfilerSetting.TraceValueDepth(1));
        }

        [TestMethod]
        public void StaticField_ComplexStructType_CustomDepth()
        {
            Test(s =>
            {
                var verifier = new ValueVerifier(s.GetStaticField("StaticFieldType.complexStructType", maxTraceDepth: 3));

                verifier.HasValueType("DebugTools.TestHost.StructWithFieldWithPrimitiveField").HasFieldValue(v =>
                {
                    v.HasValueType("DebugTools.TestHost.StructWithPrimitiveField").HasFieldValue(5);
                });
            }, ProfilerSetting.Detailed, ProfilerSetting.TraceValueDepth(1));
        }

        [TestMethod]
        public void StaticField_ThreadStatic()
        {
            Test(s =>
            {
                var thread = s.ThreadCache.Single().Key;

                //GetThreadStaticAddress requires that the specified threadId either be null or be the current thread be the same thread as threadId.
                //Either way, you can only poke thread static values from within the thread they exist on.
                AssertEx.Throws<DebugException>(
                    () => s.GetStaticField("StaticFieldType.threadStaticPrimitiveType", thread),
                    "Error HRESULT E_INVALIDARG has been returned from a call to a COM component."
                );
            }, ProfilerSetting.Detailed);
        }

        [TestMethod]
        public void StaticField_Property()
        {
            //Currently unsupported, as we can't read custom reference types from an unmanaged thread.
            //If we add support, we should support trying to automatically get the backing field if
            //a field wasn't found but a property was. Will need to document on wiki that you can specify properties
            //with auto getters.

            Test(s =>
            {
                AssertEx.Throws<ProfilerException>(
                    () => s.GetStaticField("StaticFieldType.StringPropertyType"),
                    "The specified field could not be found on the specified type."
                );
            }, ProfilerSetting.Detailed);
        }

        [TestMethod]
        public void StaticField_StructGenericInst()
        {
            Test(s =>
            {
                var verifier = new ValueVerifier(s.GetStaticField("StaticFieldType.complexGenericStructType"));

                verifier.HasValueType("DebugTools.TestHost.GenericStructType`1").HasFieldValue(6);
            }, ProfilerSetting.Detailed);
        }

        [TestMethod]
        public void StaticField_StructVarType()
        {
            Test(s =>
            {
                //Asking for a generic type will probably return the canonical type. GetAppDomainStaticAddress() will
                //get upset when you pass the classID of a canonical type in. Therefore, reading fields inside
                //generic types is not currently supported.
                AssertEx.Throws<ProfilerException>(
                    () => s.GetStaticField("GenericStructType`1.staticValue"),
                    "The CLR reported that the field cannot be inspected as it, or its containing class, have not yet been initialized."
                );
            }, ProfilerSetting.Detailed);
        }

        [TestMethod]
        public void StaticField_InvalidField_Throws()
        {
            Test(s =>
            {
                AssertEx.Throws<ProfilerException>(
                    () => s.GetStaticField("StaticFieldType.foo"),
                    "The specified field could not be found on the specified type."
                );
            }, ProfilerSetting.Detailed);
        }

        [TestMethod]
        public void StaticField_LeadingDotAndField_Throws()
        {
            Test(s =>
            {
                AssertEx.Throws<ProfilerException>(
                    () => s.GetStaticField(".foo"),
                    "The specified request to trace a static field was malformed."
                );
            }, ProfilerSetting.Detailed);
        }

        [TestMethod]
        public void StaticField_FieldAndTrailingDot_Throws()
        {
            Test(s =>
            {
                AssertEx.Throws<ProfilerException>(
                    () => s.GetStaticField("foo."),
                    "The specified request to trace a static field was malformed."
                );
            }, ProfilerSetting.Detailed);
        }

        [TestMethod]
        public void StaticField_BufferTooLarge_Throws()
        {
            Test(s =>
            {
                var str = "A." + string.Join(string.Empty, Enumerable.Range(0, 994).Select(v => 'A'));

                AssertEx.Throws<InvalidOperationException>(
                    () => s.GetStaticField(str),
                    $"Cannot invoke command GetStaticField: value '{str}|0|0' was too large."
                );
            }, ProfilerSetting.Detailed);
        }

        [TestMethod]
        public void StaticField_NullTypeAndField_Throws()
        {
            Test(s =>
            {
                AssertEx.Throws<ArgumentNullException>(
                    () => s.GetStaticField(null),
                    "Value cannot be null"
                );
            }, ProfilerSetting.Detailed);
        }

        [TestMethod]
        public void StaticField_EmptyTypeAndField_Throws()
        {
            Test(s =>
            {
                AssertEx.Throws<ArgumentException>(
                    () => s.GetStaticField(string.Empty),
                    "A type and name must be specified however was an empty string was provided."
                );
            }, ProfilerSetting.Detailed);
        }

        [TestMethod]
        public void StaticField_WithoutDetailed_Throws()
        {
            Test(s =>
            {
                AssertEx.Throws<ProfilerException>(
                    () => s.GetStaticField("foo.bar"),
                    "Tracing static field values is only supported when the profiler is launched in Detailed Mode."
                );
            });
        }

        [TestMethod]
        public void StaticField_AmbiguousType_Throws()
        {
            Test(s =>
            {
                AssertEx.Throws<ProfilerException>(
                    () => s.GetStaticField("DuplicateStructType.foo"),
                    "Multiple types matching the specified name were found. Consider specifying a namespace + type to prevent ambiguities."
                );
            }, ProfilerSetting.Detailed);
        }

        internal void Test(Action<ProfilerSession> getField, params ProfilerSetting[] settings)
        {
            var type = StaticFieldTestType.Normal;

            var settingsList = settings.ToList();
            settingsList.Add(ProfilerSetting.TraceStart);

            var config = new LiveProfilerReaderConfig(ProfilerSessionType.Normal, $"{ProfilerInfo.TestHost} {TestType.StaticField} {type}", settingsList.ToArray());

            using (var session = new ProfilerSession(config))
            {
                var waitForCompleted = new AutoResetEvent(false);
                var waitForMethod = new ManualResetEvent(false);

                session.Reader.Completed += () => waitForCompleted.Set();

                void eventHandler<T>(T args) where T : ICallArgs
                {
                    var method = session.GetMethodSafe(args.FunctionID);

                    if (method.MethodName == type.ToString())
                        waitForMethod.Set();
                }

                session.Reader.CallEnter += eventHandler;
                session.Reader.CallEnterDetailed += eventHandler;

                session.Start(default);

                var process = ((LiveProfilerTarget) session.Target).Process;

                try
                {
                    if (!waitForMethod.WaitOne(TimeSpan.FromSeconds(5)))
                        throw new TimeoutException($"Timed out waiting for method '{type}'.");

                    getField(session);
                }
                finally
                {
                    if (!process.HasExited)
                        process.Kill();

                    waitForCompleted.WaitOne();
                }
            }
        }
    }
}
