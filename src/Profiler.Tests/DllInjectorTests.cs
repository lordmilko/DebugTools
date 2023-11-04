using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ClrDebug;
using DebugTools.Host;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Profiler.Tests
{
    [TestClass]
    public class DllInjectorTests
    {
        [TestMethod]
        public void DllInjector_NetFramework()
        {
            Test("powershell", true);
        }

        [TestMethod]
        public void DllInjector_NetCore()
        {
            //PowerShell is 64-bit only
            if (IntPtr.Size == 4)
                Assert.Inconclusive();

            //.NET Remoting isn't supported in .NET Core, and we don't currently have a good alternative

            AssertEx.Throws<DebugException>(
                () => Test("pwsh", true),
                "COR_E_TYPEINITIALIZATION"
            );
        }

        [TestMethod]
        public void DllInjector_Unmanaged()
        {
            AssertEx.Throws<DebugException>(
                () => Test("wordpad", false),
                "HOST_E_CLRNOTAVAILABLE"
            );
        }

        private void Test(string processName, bool expectCLR, bool debug = false)
        {
            var process = Process.Start(processName);

            if (expectCLR)
                WaitForCLR(process);

            try
            {
                new InjectedHostSession(process, debug).Dispose();
            }
            finally
            {
                process.Kill();
            }
        }

        private void WaitForCLR(Process process)
        {
            var expected = new[]
            {
                "clr.dll",
                "coreclr.dll"
            };

            for (var i = 0; i < 10; i++)
            {
                var local = Process.GetProcessById(process.Id);

                if (local.Modules.Cast<ProcessModule>().Any(m => expected.Any(v => v.Equals(m.ModuleName, StringComparison.OrdinalIgnoreCase))))
                    return;

                Thread.Sleep(100);
            }

            Assert.Fail($"CLR was not loaded into process {process.ProcessName}");
        }
    }
}
