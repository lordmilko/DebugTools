using System.Linq;
using DebugTools.SOS;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Profiler.Tests
{
    [TestClass]
    public class GetSOSAssembly_Tests : BasePowerShellTest
    {
        [TestMethod]
        public void PowerShell_SOSAssembly_All()
        {
            TestSOS<SOSAssembly>(WellKnownCmdlet.GetSOSAssembly, null, v => Assert.IsTrue(v.Length > 0));
        }

        [TestMethod]
        public void PowerShell_SOSAssembly_Address()
        {
            SOSAssembly assembly = null;

            TestSOS<SOSAssembly>(WellKnownCmdlet.GetSOSAssembly, null, v => assembly = v[0]);

            TestSOS<SOSAssembly>(WellKnownCmdlet.GetSOSAssembly, assembly.AssemblyPtr, v => Assert.AreEqual(assembly.AssemblyPtr, v.Single().AssemblyPtr));
        }

        [TestMethod]
        public void PowerShell_SOSAssembly_Name()
        {
            SOSAssembly assembly = null;

            TestSOS<SOSAssembly>(WellKnownCmdlet.GetSOSAssembly, null, v => assembly = v[0]);

            TestSOS<SOSAssembly>(WellKnownCmdlet.GetSOSAssembly, assembly.Name, v => Assert.IsTrue(v.All(a => assembly.AssemblyPtr == a.AssemblyPtr)));
        }

        [TestMethod]
        public void PowerShell_SOSAssembly_FromAppDomain()
        {
            TestSOS<SOSAssembly>(WellKnownCmdlet.GetSOSAssembly, null, WellKnownCmdlet.GetSOSAppDomain, v => Assert.IsTrue(v.Length > 0));
        }
    }
}
