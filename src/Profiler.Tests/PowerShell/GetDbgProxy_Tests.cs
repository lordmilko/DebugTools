using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Management.Automation;
using System.Security;
using System.Threading;
using DebugTools.Dynamic;
using DebugTools.Host;
using DebugTools.Profiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Profiler.Tests
{
    [TestClass]
    public class GetDbgProxy_Tests : BaseTest
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void PowerShell_DbgProxy_GetField_Primitive()
        {
            Test("*TestHost*", "*StaticFieldType", "primitiveType", v => Assert.AreEqual(3, (int) v));
        }

        [TestMethod]
        public void PowerShell_DbgProxy_GetField_OnGenericType()
        {
            Test("*TestHost*", "*GenericStructType", validate: v => Assert.AreEqual("Cannot get generic type static field 'DebugTools.TestHost.GenericStructType<T>.staticValue'", (string)v));
        }

        [TestMethod]
        public void PowerShell_DbgProxy_GetField_Class()
        {
            Test("*TestHost*", "*StaticFieldType", "complexClassType", v => Assert.AreEqual(4, (int) v.field1.field1));
        }

        [TestMethod]
        public void PowerShell_DbgProxy_GetProperty_Primitive()
        {
            Test("*TestHost*", "*StaticFieldType", "StringPropertyType", v => Assert.AreEqual("bar", (string) v));
        }

        [TestMethod]
        public void PowerShell_DbgProxy_GetProperty_Class()
        {
            Test("*TestHost*", "*StaticFieldType", "ClassPropertyType", v => Assert.AreEqual(4, (int) v.field1.field1));
        }

        [TestMethod]
        public void PowerShell_DbgProxy_CallMethod_ReturnPrimitive()
        {
            Test("*TestHost*", "*StaticFieldType", "Methods", v => Assert.AreEqual(5, (int) v.MethodReturnPrimitive()));
        }

        [TestMethod]
        public void PowerShell_DbgProxy_CallMethod_ReturnClass()
        {
            Test("*TestHost*", "*StaticFieldType", "Methods", v => Assert.AreEqual(4, (int) v.MethodReturnClass().field1.field1));
        }

        [TestMethod]
        public void PowerShell_DbgProxy_GetIndexer_Primitive()
        {
            Test("*TestHost*", "*StaticFieldType", "Methods", v =>
            {
                AssertEx.Throws<NotImplementedException>(
                    () => Assert.AreEqual(12, (int)v[4]),
                    "Disambiguating between multiple potentially valid indexer overloads is not implemented"
                );
            });
        }

        [TestMethod]
        public void PowerShell_DbgProxy_SetIndexer_Primitive()
        {
            Test("*TestHost*", "*StaticFieldType", "Methods", v =>
            {
                AssertEx.Throws<NotImplementedException>(
                    () =>
                    {
                        Assert.AreEqual(12, (int)v[4]);
                        v[4] = 2;
                        Assert.AreEqual(8, (int)v[4]);
                    },
                    "Disambiguating between multiple potentially valid indexer overloads is not implemented"
                );
            });
        }

        [TestMethod]
        public void PowerShell_DbgProxy_GetIndexer_WithEnum()
        {
            Test("*TestHost*", "*StaticFieldType", "Methods", v =>
            {
                var value = v[DayOfWeek.Wednesday];

                Assert.AreEqual(3, value.Day);
            });
        }

        [TestMethod]
        public void PowerShell_DbgProxy_GetIndexer_WithEnumString()
        {
            Test("*TestHost*", "*StaticFieldType", "Methods", v =>
            {
                //Not currently supported
                AssertEx.Throws<NotImplementedException>(
                    () =>
                    {
                        var value = v["Wednesday"];

                        Assert.AreEqual(3, value.Day);
                    },
                    "Disambiguating between multiple potentially valid indexer overloads is not implemented"
                );
            });
        }

        [TestMethod]
        public void PowerShell_DbgProxy_Enumerable()
        {
            Test("*TestHost*", "*StaticFieldType", "Methods", v =>
            {
                var value = v.MethodReturnEnumerable();

                var list = ((IEnumerable<object>)value).ToArray();

                Assert.AreEqual(2, list.Length);
                Assert.AreEqual(1, list[0]);
                Assert.AreEqual(2, list[1]);
            });
        }

        [TestMethod]
        public void PowerShell_DbgProxy_Dictionary()
        {
            Test("*TestHost*", "*StaticFieldType", "Methods", v =>
            {
                var value = v.MethodReturnDictionary();

                var list = ((IEnumerable<object>)value).Cast<KeyValuePair<string, string>>().ToArray();

                Assert.AreEqual(2, list.Length);
                Assert.AreEqual("a", list[0].Key);
                Assert.AreEqual("b", list[0].Value);
                Assert.AreEqual("c", list[1].Key);
                Assert.AreEqual("d", list[1].Value);
            });
        }

        [TestMethod]
        public void PowerShell_DbgProxy_ExternalType_Array_Enum_NoMembers()
        {
            TestOutOfProc(proxy =>
            {
                var items = ((IEnumerable<object>)proxy).ToArray();

                var first = items[0];
                Assert.AreEqual("Id", first.ToString());

                var metaObject = ((LocalProxyStub)first).GetMetaObject(Expression.Parameter(typeof(LocalProxyStub)));
                var members = metaObject.GetDynamicMemberNames();

                Assert.AreEqual(0, members.Count());
            });
        }

        [TestMethod]
        public void PowerShell_DbgProxy_GetType_Name()
        {
            TestOutOfProc(proxy =>
            {
                var items = ((IEnumerable<object>)proxy).ToArray();

                var first = (dynamic) items[0];
                Assert.AreEqual("Id", first.ToString());

                var type = first.GetType();

                Assert.AreEqual("Property", type.Name);
            });
        }

        private void Test(
            string assemblyName = null,
            string typeName = null,
            string fieldName = null,
            Action<dynamic> validate = null)
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

                if (warnings.Length == 1)
                    validate(warnings[0]);
                else
                {
                    Assert.AreEqual(1, fields.Length, "Did not have correct number of fields");

                    var field = fields.Single();

                    var proxy = invoker.Invoke<object>("Get-DbgProxy", new { Field = field }).Single();

                    validate(proxy);
                }                
            }
            finally
            {
                process.Kill();
            }
        }

        private void TestOutOfProc(Action<dynamic> validate)
        {
            var outOfProc = new OutOfProcPowerShellInvoker();

            var ss = new SecureString();

            foreach (var c in "prtgadmin".ToCharArray())
                ss.AppendChar(c);

            outOfProc.Invoke<object>("Connect-PrtgServer", new { Server = "http://prtg.example.com", Credential = new PSCredential("prtgadmin", ss), PassHash = true, PassThru = true });

            var invoker = new PowerShellInvoker();

            var fields = invoker.Invoke<StaticFieldInfo>(
                "Get-DbgStaticField",
                new
                {
                    TypeName = "*prtg*",
                    FieldName = "*default*",
                    ProcessId = outOfProc.Process.Id
                }
            );

            var deviceProperties = fields.First();
            Assert.AreEqual("PrtgAPI.Parameters.DeviceParameters.defaultProperties", deviceProperties.ToString());

            var proxy = invoker.Invoke<object>("Get-DbgProxy", new { Field = deviceProperties }).Single(); //dbg = true - temp

            try
            {
                validate(proxy);
            }
            finally
            {
                outOfProc.Process.Kill();
            }
        }
    }
}
