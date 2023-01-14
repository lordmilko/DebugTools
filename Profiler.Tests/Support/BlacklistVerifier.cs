using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Profiler.Tests
{
    internal struct BlacklistVerifier
    {
        private string[] moduleNames;

        public BlacklistVerifier(string[] moduleNames)
        {
            //Methods are stored in a ConcurrentBag, so have non-deterministic ordering
            this.moduleNames = moduleNames.OrderBy(v => v).ToArray();
        }

        public void HasModules(params string[] expected)
        {
            var err = $"\n\nExpected: {string.Join(", ", expected)}\n\nActual: {string.Join(", ", moduleNames)}";

            Assert.AreEqual(expected.Length, moduleNames.Length, err);

            for (var i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], moduleNames[i], err);
            }
        }
    }
}
