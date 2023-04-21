using System.Linq;
using DebugTools.SOS;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Profiler.Tests
{
    [TestClass]
    public class GetSOSMethodTable_Tests : BasePowerShellTest
    {
        [TestMethod]
        public void PowerShell_SOSMethodTable_All()
        {
            TestSOS<SOSMethodTable>(WellKnownCmdlet.GetSOSMethodTable, null, v => Assert.IsTrue(v.Length > 0));
        }

        [TestMethod]
        public void PowerShell_SOSMethodTable_Address()
        {
            SOSMethodTable methodTable = null;

            TestSOS<SOSMethodTable>(WellKnownCmdlet.GetSOSMethodTable, null, v => methodTable = v[0]);

            TestSOS<SOSMethodTable>(WellKnownCmdlet.GetSOSMethodTable, methodTable.Address, v => Assert.AreEqual(methodTable.Address, v.Single().Address));
        }

        [TestMethod]
        public void PowerShell_SOSMethodTable_Name()
        {
            SOSMethodTable methodTable = null;

            TestSOS<SOSMethodTable>(WellKnownCmdlet.GetSOSMethodTable, null, v => methodTable = v.Last());

            TestSOS<SOSMethodTable>(WellKnownCmdlet.GetSOSMethodTable, methodTable.Name, v => Assert.IsTrue(v.All(m => methodTable.Name == m.Name)));
        }

        [TestMethod]
        public void PowerShell_SOSMethodTable_FromAppDomain()
        {
            TestSOS<SOSMethodTable>(WellKnownCmdlet.GetSOSMethodTable, null, WellKnownCmdlet.GetSOSAppDomain, v => Assert.IsTrue(v.Length > 0));
        }

        [TestMethod]
        public void PowerShell_SOSMethodTable_FromAssembly()
        {
            TestSOS<SOSMethodTable>(WellKnownCmdlet.GetSOSMethodTable, null, WellKnownCmdlet.GetSOSAssembly, v => Assert.IsTrue(v.Length > 0));
        }

        [TestMethod]
        public void PowerShell_SOSMethodTable_FromModule()
        {
            TestSOS<SOSMethodTable>(WellKnownCmdlet.GetSOSMethodTable, null, WellKnownCmdlet.GetSOSModule, v => Assert.IsTrue(v.Length > 0));
        }
    }
}
