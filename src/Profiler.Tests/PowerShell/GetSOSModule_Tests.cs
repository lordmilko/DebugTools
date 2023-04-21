using System.Linq;
using DebugTools.SOS;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Profiler.Tests
{
    [TestClass]
    public class GetSOSModule_Tests : BasePowerShellTest
    {
        [TestMethod]
        public void PowerShell_SOSModule_All()
        {
            TestSOS<SOSModule>(WellKnownCmdlet.GetSOSModule, null, v => Assert.IsTrue(v.Length > 0));
        }

        [TestMethod]
        public void PowerShell_SOSModule_Address()
        {
            SOSModule module = null;

            TestSOS<SOSModule>(WellKnownCmdlet.GetSOSModule, null, v => module = v[0]);

            TestSOS<SOSModule>(WellKnownCmdlet.GetSOSModule, module.Address, v => Assert.AreEqual(module.Address, v.Single().Address));
        }

        [TestMethod]
        public void PowerShell_SOSModule_Name()
        {
            SOSModule module = null;

            TestSOS<SOSModule>(WellKnownCmdlet.GetSOSModule, null, v => module = v[0]);

            TestSOS<SOSModule>(WellKnownCmdlet.GetSOSModule, module.FileName, v => Assert.IsTrue(v.All(m => m.FileName == module.FileName)));
        }

        [TestMethod]
        public void PowerShell_SOSModule_FromAppDomain()
        {
            TestSOS<SOSModule>(WellKnownCmdlet.GetSOSModule, null, WellKnownCmdlet.GetSOSAppDomain, v => Assert.IsTrue(v.Length > 0));
        }

        [TestMethod]
        public void PowerShell_SOSModule_FromAssembly()
        {
            TestSOS<SOSModule>(WellKnownCmdlet.GetSOSModule, null, WellKnownCmdlet.GetSOSAssembly, v => Assert.IsTrue(v.Length > 0));
        }
    }
}
