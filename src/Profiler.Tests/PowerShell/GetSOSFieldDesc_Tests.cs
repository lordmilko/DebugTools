using System;
using System.Linq;
using DebugTools.SOS;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Profiler.Tests
{
    [TestClass]
    public class GetSOSFieldDesc_Tests : BasePowerShellTest
    {
        [TestMethod]
        public void PowerShell_SOSFieldDesc_All()
        {
            TestSOS<SOSFieldDesc>(WellKnownCmdlet.GetSOSFieldDesc, null, v => Assert.IsTrue(v.Length > 0));
        }

        [TestMethod]
        public void PowerShell_SOSFieldDesc_Address()
        {
            SOSFieldDesc field = null;

            TestSOS<SOSFieldDesc>(WellKnownCmdlet.GetSOSFieldDesc, null, v => field = v[0]);

            TestSOS<SOSFieldDesc>(WellKnownCmdlet.GetSOSFieldDesc, field.Address, v => Assert.AreEqual(field.Address, v.Single().Address));
        }

        [TestMethod]
        public void PowerShell_SOSFieldDesc_Name()
        {
            SOSFieldDesc field = null;

            TestSOS<SOSFieldDesc>(WellKnownCmdlet.GetSOSFieldDesc, null, v => field = v[0]);

            TestSOS<SOSFieldDesc>(WellKnownCmdlet.GetSOSFieldDesc, field.Name, v =>
            {
                Assert.IsTrue(v.Length > 0, "Did not have any results");
                Assert.IsTrue(v.All(f => StringComparer.OrdinalIgnoreCase.Equals(f.Name, field.Name)));
            });
        }

        [TestMethod]
        public void PowerShell_SOSFieldDesc_FromAppDomain()
        {
            TestSOS<SOSFieldDesc>(WellKnownCmdlet.GetSOSFieldDesc, null, WellKnownCmdlet.GetSOSAppDomain, v => Assert.IsTrue(v.Length > 0));
        }

        [TestMethod]
        public void PowerShell_SOSFieldDesc_FromAssembly()
        {
            TestSOS<SOSFieldDesc>(WellKnownCmdlet.GetSOSFieldDesc, null, WellKnownCmdlet.GetSOSAssembly, v => Assert.IsTrue(v.Length > 0));
        }

        [TestMethod]
        public void PowerShell_SOSFieldDesc_FromModule()
        {
            TestSOS<SOSFieldDesc>(WellKnownCmdlet.GetSOSFieldDesc, null, WellKnownCmdlet.GetSOSModule, v => Assert.IsTrue(v.Length > 0));
        }

        [TestMethod]
        public void PowerShell_SOSFieldDesc_FromMethodTable()
        {
            TestSOS<SOSFieldDesc>(WellKnownCmdlet.GetSOSFieldDesc, null, WellKnownCmdlet.GetSOSMethodTable, v => Assert.IsTrue(v.Length > 0));
        }
    }
}
