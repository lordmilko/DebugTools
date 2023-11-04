using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using DebugTools.Host;
using DebugTools.Profiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Profiler.Tests
{
    [TestClass]
    public class GetDbgStaticField_Tests : BaseTest
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void PowerShell_DbgStaticField_Success()
        {
            Test("*TestHost*", "*StaticFieldType", "primitiveType", validate: (f, w) =>
            {
                Assert.AreEqual("DebugTools.TestHost.StaticFieldType.primitiveType", f.Single().ToString());
            });
        }

        [TestMethod]
        public void PowerShell_DbgStaticField_OnGenericType()
        {
            Test("*TestHost*", "*GenericStructType", validate: (f, w) =>
            {
                Assert.AreEqual(
                    "Cannot get static generic 'DebugTools.TestHost.GenericStructType<T>.staticValue'",
                    w.Single()
                );
            });
        }

        [TestMethod]
        public void PowerShell_DbgStaticField_Type_WithNamespace()
        {
            Test("*TestHost*", "DebugTools.TestHost.StaticFieldType", "primitiveType", validate: (f, w) =>
            {
                Assert.AreEqual("DebugTools.TestHost.StaticFieldType.primitiveType", f.Single().ToString());
            });
        }

        [TestMethod]
        public void PowerShell_DbgStaticField_Type_WithoutNamespace()
        {
            Test("*TestHost*", "StaticFieldType", "primitiveType", validate: (f, w) =>
            {
                Assert.AreEqual("DebugTools.TestHost.StaticFieldType.primitiveType", f.Single().ToString());
            });
        }

        private void Test(
            string assemblyName = null,
            string typeName = null,
            string fieldName = null,
            Action<StaticFieldInfo[], string[]> validate = null)
        {
            if (validate == null)
                throw new ArgumentNullException(nameof(validate));

            var eventName = $"DebugTools_Test_{Process.GetCurrentProcess().Id}_{TestContext.TestName}";

            var process = Process.Start(ProfilerInfo.TestHost, $"{TestType.StaticField} {StaticFieldTestType.NotifyNormal} {eventName}");

            using var eventHandle = new EventWaitHandle(false, EventResetMode.ManualReset, eventName);

            try
            {
                eventHandle.WaitOne();

                var invoker = new PowerShellInvoker();

                var fields = invoker.Invoke<StaticFieldInfo>(
                    "Get-DbgStaticField",
                    new
                    {
                        AssemblyName = assemblyName,
                        TypeName = typeName,
                        FieldName = fieldName,
                        ProcessId = process.Id
                    }
                );

                var warnings = invoker.GetWarnings();

                validate(fields, warnings);
            }
            finally
            {
                process.Kill();
            }
        }
    }
}
