using System;
using System.Linq;
using DebugTools.SOS;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Profiler.Tests
{
    [TestClass]
    public class GetSOSMethodDesc_Tests : BasePowerShellTest
    {
        [TestMethod]
        public void PowerShell_SOSMethodDesc_All()
        {
            TestSOS<SOSMethodDesc>(WellKnownCmdlet.GetSOSMethodDesc, null, v => Assert.IsTrue(v.Length > 0));
        }

        [TestMethod]
        public void PowerShell_SOSMethodDesc_Address()
        {
            SOSMethodDesc method = null;

            TestSOS<SOSMethodDesc>(WellKnownCmdlet.GetSOSMethodDesc, null, v => method = v[0]);

            TestSOS<SOSMethodDesc>(WellKnownCmdlet.GetSOSMethodDesc, method.MethodDescPtr, v => Assert.AreEqual(method.Name, v.Single().Name));
        }

        [TestMethod]
        public void PowerShell_SOSMethodDesc_Name()
        {
            SOSMethodDesc method = null;

            TestSOS<SOSMethodDesc>(WellKnownCmdlet.GetSOSMethodDesc, null, v => method = v.Last());

            TestSOS<SOSMethodDesc>(WellKnownCmdlet.GetSOSMethodDesc, method.Name, v =>
            {
                Assert.IsTrue(v.Length > 0, "Did not have any results");
                Assert.IsTrue(v.All(m => StringComparer.OrdinalIgnoreCase.Equals(m.Name, method.Name)));
            });
        }

        [TestMethod]
        public void PowerShell_SOSMethodDesc_FromAppDomain()
        {
            TestSOS<SOSMethodDesc>(WellKnownCmdlet.GetSOSMethodDesc, null, WellKnownCmdlet.GetSOSAppDomain, v => Assert.IsTrue(v.Length > 0));
        }

        [TestMethod]
        public void PowerShell_SOSMethodDesc_FromAssembly()
        {
            TestSOS<SOSMethodDesc>(WellKnownCmdlet.GetSOSMethodDesc, null, WellKnownCmdlet.GetSOSAssembly, v => Assert.IsTrue(v.Length > 0));
        }

        [TestMethod]
        public void PowerShell_SOSMethodDesc_FromModule()
        {
            TestSOS<SOSMethodDesc>(WellKnownCmdlet.GetSOSMethodDesc, null, WellKnownCmdlet.GetSOSModule, v => Assert.IsTrue(v.Length > 0));
        }

        [TestMethod]
        public void PowerShell_SOSMethodDesc_FromMethodTable()
        {
            TestSOS<SOSMethodDesc>(WellKnownCmdlet.GetSOSMethodDesc, null, WellKnownCmdlet.GetSOSMethodTable, v => Assert.IsTrue(v.Length > 0));
        }
    }
}
