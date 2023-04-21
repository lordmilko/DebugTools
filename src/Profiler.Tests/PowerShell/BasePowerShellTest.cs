using System;
using System.Diagnostics;
using DebugTools.PowerShell;
using DebugTools.Profiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Profiler.Tests
{
    public class BasePowerShellTest : BaseTest
    {
        protected void TestProfiler<T>(string cmdlet, object param, RootFrame root, T[] expected, params Func<T, T, bool>[] validate) =>
            TestProfiler(cmdlet, param, new[] { root }, expected, validate);

        protected void TestProfiler<T>(string cmdlet, object param, RootFrame[] root, T[] expected, params Func<T, T, bool>[] validate)
        {
            var actual = InvokeProfiler<T>(cmdlet, param, root);

            if (actual.Length != expected.Length)
                Assert.Fail($"Expected frames: {expected.Length}. Actual: {actual.Length}");

            for (var i = 0; i < actual.Length; i++)
            {
                for (var j = 0; j < validate.Length; j++)
                {
                    Assert.IsTrue(validate[j](expected[i], actual[i]), $"'{expected[i]}' did not equal '{actual[i]}' in the correct way (Frame {i}, Validation {j})");
                }
            }
        }

        protected void TestSOS<T>(string cmdlet, object param, Action<T[]> validate)
        {
            var result = InvokeSOS<T>(cmdlet, param);

            validate(result);
        }

        protected void TestSOS<T>(string cmdlet, object param, string inputCmdlet, Action<T[]> validate)
        {
            var result = InvokeSOS<T>(cmdlet, param, inputCmdlet);

            validate(result);
        }

        protected T[] InvokeProfiler<T>(string cmdlet, object param, params RootFrame[] roots)
        {
            var session = new MockProfilerSession(roots);

            var invoker = new PowerShellInvoker();

            var results = invoker.Invoke<T>(cmdlet, param, new { Session = session }, null);

            return results;
        }

        protected T[] InvokeSOS<T>(string cmdlet, object param, string inputCmdlet = null)
        {
            var sosProcess = AcquireSOSProcess();

            var invoker = new PowerShellInvoker();

            var results = invoker.Invoke<T>(cmdlet, param, new { Process = sosProcess }, inputCmdlet);

            return results;
        }

        private static object objLock = new object();
        private static LocalSOSProcess cachedSOSProcess;

        private LocalSOSProcess AcquireSOSProcess()
        {
            lock (objLock)
            {
                if (cachedSOSProcess == null)
                {
                    //CreateSOSProcess will store the process in a dictionary for lookup later on
                    var process = Process.GetCurrentProcess();
                    var hostApp = DebugToolsSessionState.GetDetectedHost(process);
                    var handle = hostApp.CreateSOSProcess(process.Id, true);

                    var sosProcess = new LocalSOSProcess(handle);

                    cachedSOSProcess = sosProcess;
                }

                return cachedSOSProcess;
            }
        }
    }
}
