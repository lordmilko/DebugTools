using System;
using System.Diagnostics;
using System.Threading;
using ClrDebug;
using DebugTools.Memory;
using DebugTools.Profiler;
using DebugTools.SOS;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ClrDebug.Extensions;

namespace Profiler.Tests
{
    [TestClass]
    public class SOSTests
    {
        [TestMethod]
        public void SOS_AppDomain()
        {
            Test(sos =>
            {
                var provider = new SOSProvider(sos);

                Assert.AreEqual(3, provider.AppDomains.Length);
            });
        }

        [TestMethod]
        public void SOS_Assembly()
        {
            Test(sos =>
            {
                var provider = new SOSProvider(sos);

                Assert.IsTrue(provider.Assemblies.Length > 0);
            });
        }

        [TestMethod]
        public void SOS_Module()
        {
            Test(sos =>
            {
                var provider = new SOSProvider(sos);

                Assert.IsTrue(provider.Modules.Length > 0);
            });
        }

        [TestMethod]
        public void SOS_MethodTable()
        {
            Test(sos =>
            {
                var provider = new SOSProvider(sos);

                Assert.IsTrue(provider.MethodTables.Length > 0);
            });
        }

        [TestMethod]
        public void SOS_MethodDesc()
        {
            Test(sos =>
            {
                var provider = new SOSProvider(sos);

                Assert.IsTrue(provider.MethodDescs.Length > 0);
            });
        }

        [TestMethod]
        public void SOS_FieldDesc()
        {
            Test(sos =>
            {
                var provider = new SOSProvider(sos);

                Assert.IsTrue(provider.FieldDescs.Length > 0);
            });
        }

        private void Test(Action<SOSDacInterface> action)
        {
            var process = Process.Start(ProfilerInfo.TestHost, $"{TestType.SOS} {Process.GetCurrentProcess().Id}");

            try
            {
                var sos = CLRDataCreateInstance(new DataTarget(process)).SOSDacInterface;

                Exception exception = null;

                //If we query too quickly after the process has started, the relevant structures may not
                //have loaded within the target process yet yet.
                for (var i = 0; i < 100; i++)
                {
                    if (process.HasExited)
                        throw exception;

                    try
                    {
                        action(sos);
                        break;
                    }
                    catch (AssertFailedException ex)
                    {
                        if (exception == null)
                            exception = ex;
                    }
                }

                Thread.Sleep(10);
            }
            finally
            {
                process.Kill();
            }
        }
    }
}
