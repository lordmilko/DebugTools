using System;
using System.Linq;
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

        [TestMethod]
        public void Profiler_Async()
        {
            Test(ProfilerTestType.Async, v =>
            {
                v.HasFrame("Async");
            });
        }

        [TestMethod]
        public void Profiler_Thread_NameAfterCreate()
        {
            Test(ProfilerTestType.Thread_NameAfterCreate, v =>
            {
                var thread = v.FindThread("NameAfterCreate").Verify();

                thread.HasFrame("Thread_NameAfterCreate");
            });
        }

        [TestMethod]
        public void Profiler_Thread_NameBeforeCreate()
        {
            Test(ProfilerTestType.Thread_NameBeforeCreate, v =>
            {
                var thread = v.FindThread("NameBeforeCreate").Verify();

                thread.HasFrame("Thread_NameBeforeCreate");
            });
        }

        [TestMethod]
        public void Profiler_Thread_NamedAndNeverStarted()
        {
            Test(ProfilerTestType.Thread_NamedAndNeverStarted, v =>
            {
                var match = v.ThreadStacks.SingleOrDefault(t => t.Root.ThreadName == "Thread_NameBeforeCreate");

                Assert.IsNull(match);
            });
        }

        [TestMethod]
        public void Profiler_DynamicModule()
        {
            Test(ProfilerTestType.DynamicModule, v =>
            {
                var dynamicMethod = (IMethodInfoDetailed) v.Methods.First(m => m.ModuleName == "Microsoft.GeneratedCode");

                Assert.IsNull(dynamicMethod.SigMethod);
            }, ProfilerSetting.Detailed);
        }

        internal void Test(ProfilerTestType type, Action<Validator> validate, params ProfilerSetting[] settings) =>
            TestInternal(TestType.Profiler, type.ToString(), validate, settings);
    }
}
