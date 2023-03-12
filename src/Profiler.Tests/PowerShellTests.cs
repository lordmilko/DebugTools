using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using DebugTools.PowerShell.Cmdlets;
using DebugTools.Profiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Profiler.Tests.ValueFactory;

namespace Profiler.Tests
{
    [TestClass]
    public class PowerShellTests : BaseTest
    {
        [TestMethod]
        public void PowerShell_GetFrames_NoArgs()
        {
            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", Boolean(true)),
                    MakeFrame("second", Boolean(false))
                )
            );

            var expected = Flatten(tree);

            Test(new GetDbgProfilerStackFrame(), tree, expected, ReferenceEquals);
        }

        [TestMethod]
        public void PowerShell_GetFrames_Include()
        {
            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", Boolean(true)),
                    MakeFrame("second", Boolean(false))
                )
            );

            var expected = Flatten(tree).Take(1).ToArray();

            Test(new GetDbgProfilerStackFrame
            {
                Include = new[] {"*first*"}
            }, tree, expected, ReferenceEquals);
        }

        [TestMethod]
        public void PowerShell_GetFrames_IncludeAll()
        {
            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", Boolean(true)),
                    MakeFrame("second", Boolean(false))
                )
            );

            var expected = Flatten(tree);

            Test(new GetDbgProfilerStackFrame
            {
                Include = new[] { "*" }
            }, tree, expected, ReferenceEquals);
        }

        [TestMethod]
        public void PowerShell_GetFrames_Unique()
        {
            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", Boolean(true)),
                    MakeFrame("second", Boolean(false))
                )
            );

            var expected = Flatten(tree).Take(2).ToArray();

            //Still ReferenceEquals because Get-DbgProfilerStackFrame doesn't need to rewrite the tree
            Test(new GetDbgProfilerStackFrame {Unique = true}, tree, expected, FrameEqualityComparer.Instance.Equals, ReferenceEquals);
        }

        [TestMethod]
        public void PowerShell_GetFrames_CalledFrom()
        {
            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", Boolean(true)),
                    MakeFrame("second", Boolean(false))
                )
            );

            var expected = Flatten(tree).Skip(1).ToArray();

            Test(new GetDbgProfilerStackFrame
            {
                CalledFrom = new[] { "*first*" }
            }, tree, expected, ReferenceEquals);
        }

        [TestMethod]
        public void PowerShell_GetFrames_CalledFromAll()
        {
            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", Boolean(true)),
                    MakeFrame("second", Boolean(false))
                )
            );

            var expected = Flatten(tree).Skip(1).ToArray();

            Test(new GetDbgProfilerStackFrame
            {
                CalledFrom = new[] { "*" }
            }, tree, expected, ReferenceEquals);
        }

        private void Test<T>(ProfilerSessionCmdlet instance, RootFrame root, T[] expected, params Func<T, T, bool>[] validate)
        {
            var actual = Invoke(instance, root).Cast<T>().ToArray();

            if (actual.Length != expected.Length)
                Assert.Fail($"Expected frames: {expected.Length}. Actual: {actual.Length}");

            for (var i = 0; i < actual.Length; i++)
            {
                for (var j = 0; j < validate.Length; j++)
                {
                    Assert.IsTrue(validate[j](expected[i], actual[i]), $"'{expected[i]}' did not equal '{actual[i]}' in the correct way (Frame {i}, Validation {j})");
                }
            }
        }

        private object[] Invoke(ProfilerSessionCmdlet instance, params RootFrame[] roots)
        {
            instance.Session = new MockProfilerSession(roots);

            var flags = BindingFlags.Instance | BindingFlags.NonPublic;

            var type = instance.GetType();

            var beginProcessing = type.GetMethod("BeginProcessing", flags);
            var processRecord = type.GetMethod("ProcessRecord", flags);
            var endProcessing = type.GetMethod("EndProcessing", flags);

            var runtimeType = typeof(Cmdlet).Assembly.GetType("System.Management.Automation.DefaultCommandRuntime");

            var results = new List<object>();
            var runtime = (ICommandRuntime) Activator.CreateInstance(runtimeType, new object[] {results});

            instance.CommandRuntime = runtime;

            beginProcessing.Invoke(instance, null);
            processRecord.Invoke(instance, null);
            endProcessing.Invoke(instance, null);

            return results.ToArray();
        }

        private IFrame[] Flatten(IFrame frame) => FlattenInternal(frame).ToArray();

        private IEnumerable<IFrame> FlattenInternal(IFrame frame)
        {
            if (!(frame is IRootFrame))
                yield return frame;

            foreach (var child in frame.Children)
            {
                foreach (var item in FlattenInternal(child))
                    yield return item;
            }
        }
    }
}
