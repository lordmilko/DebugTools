using System.Linq;
using DebugTools.SOS;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Profiler.Tests
{
    [TestClass]
    public class GetSOSAppDomain_Tests : BasePowerShellTest
    {
        [TestMethod]
        public void PowerShell_SOSAppDomain_All()
        {
            TestSOS<SOSAppDomain>(WellKnownCmdlet.GetSOSAppDomain, null, v => Assert.IsTrue(v.Length > 0));
        }

        [TestMethod]
        public void PowerShell_SOSAppDomain_Address()
        {
            SOSAppDomain domain = null;

            TestSOS<SOSAppDomain>(WellKnownCmdlet.GetSOSAppDomain, null, v => domain = v[0]);

            TestSOS<SOSAppDomain>(
                WellKnownCmdlet.GetSOSAppDomain,
                new { Address = domain.AppDomainPtr },
                v => Assert.AreEqual(domain.AppDomainPtr, v.Single().AppDomainPtr)
            );
        }

        [TestMethod]
        public void PowerShell_SOSAppDomain_Type()
        {
            TestSOS<SOSAppDomain>(WellKnownCmdlet.GetSOSAppDomain, new { Type = AppDomainType.Normal }, v =>
            {
                Assert.IsTrue(v.Length > 0);

                Assert.IsTrue(v.All(a => a.Type == AppDomainType.Normal));
            });
        }
    }
}
