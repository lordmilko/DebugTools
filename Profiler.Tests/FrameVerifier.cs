using System.Linq;
using DebugTools.Profiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Profiler.Tests
{
    internal struct FrameVerifier
    {
        private IFrame frame;

        public FrameVerifier(IFrame frame)
        {
            this.frame = frame;
        }

        public void HasFrame(string name)
        {
            var child = frame.Children.FirstOrDefault(m => m.MethodInfo.MethodName == name);

            if (child == null)
                Assert.Fail($"Failed to find child frame '{name}'.");
        }

        public void HasFrames(params string[] names)
        {
            Assert.AreEqual(names.Length, frame.Children.Count, "Expected number of child frames was not correct");

            for (var i = 0; i < names.Length; i++)
                Assert.AreEqual(names[i], frame.Children[i].MethodInfo.MethodName);
        }
    }
}