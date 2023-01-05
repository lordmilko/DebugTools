using System;
using DebugTools.Profiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Profiler.Tests
{
    [TestClass]
    public class ProfilerTests : BaseTest
    {
        [TestMethod]
        public void Profiler_NoArgs() =>
            Test(ProfilerTestType.NoArgs, v => v.HasFrame("NoArgs"));

        [TestMethod]
        public void Profiler_SingleChild()
        {
            Test(ProfilerTestType.SingleChild, v =>
            {
                var frame = v.FindFrame("SingleChild");

                frame.Verify().HasFrame("SingleChild1");
            });
        }

        [TestMethod]
        public void Profiler_TwoChildren()
        {
            Test(ProfilerTestType.TwoChildren, v =>
            {
                var frame = v.FindFrame("TwoChildren");

                frame.Verify().HasFrames("TwoChildren1", "TwoChildren2");
            });
        }


        [TestMethod]
        public void Profiler_Detailed_NoArgs()
        {
            Test(ProfilerTestType.NoArgs, v =>
            {
                v.HasFrame("NoArgs");
            }, ProfilerSetting.Detailed);
        }

        internal void Test(ProfilerTestType type, Action<Validator> validate, params ProfilerSetting[] settings) =>
            TestInternal(TestType.Profiler, type.ToString(), validate, settings);
    }
}
