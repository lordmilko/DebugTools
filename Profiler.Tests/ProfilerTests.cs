using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using DebugTools;
using DebugTools.Profiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Profiler.Tests
{
    [TestClass]
    public class ProfilerTests
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

        private void Test(ProfilerTestType type, Action<Validator> validate)
        {
            using (var session = new ProfilerSession())
            {
                var flags = new List<ProfilerEnvFlags>();

                var wait = new AutoResetEvent(false);

                session.TraceEventSession.Source.Completed += () => wait.Set();

                session.Start(CancellationToken.None, $"{ProfilerInfo.TestHost} {type}", flags.ToArray(), true);

                session.Process.WaitForExit();

                wait.WaitOne();

                var threadStacks = session.ThreadCache.Values.ToArray();
                var methods = session.Methods.Values.ToArray();

                var validator = new Validator(threadStacks, methods);

                validate(validator);
            }
        }
    }
}
