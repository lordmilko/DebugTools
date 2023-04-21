using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DebugTools.Profiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Profiler.Tests
{
    [TestClass]
    public class GetDbgProfilerStackFrame_Tests : BasePowerShellTest
    {
        [TestMethod]
        public void PowerShell_GetFrames_NoArgs()
        {
            var tree = MakeRoot(
                MakeFrame("first", ValueFactory.String("aaa"),
                    MakeFrame("second", ValueFactory.Boolean(true)),
                    MakeFrame("second", ValueFactory.Boolean(false))
                )
            );

            var expected = Flatten(tree);

            TestProfiler(
                "Get-DbgProfilerStackFrame",
                null,
                tree,
                expected,
                ReferenceEquals
            );
        }

        [TestMethod]
        public void PowerShell_GetFrames_Include()
        {
            var tree = MakeRoot(
                MakeFrame("first", ValueFactory.String("aaa"),
                    MakeFrame("second", ValueFactory.Boolean(true)),
                    MakeFrame("second", ValueFactory.Boolean(false))
                )
            );

            var expected = Flatten(tree).Take(1).ToArray();

            TestProfiler(
                "Get-DbgProfilerStackFrame",
                new { Include = "*first*" },
                tree,
                expected,
                ReferenceEquals
            );
        }

        [TestMethod]
        public void PowerShell_GetFrames_IncludeAll()
        {
            var tree = MakeRoot(
                MakeFrame("first", ValueFactory.String("aaa"),
                    MakeFrame("second", ValueFactory.Boolean(true)),
                    MakeFrame("second", ValueFactory.Boolean(false))
                )
            );

            var expected = Flatten(tree);

            TestProfiler(
                "Get-DbgProfilerStackFrame",
                new { Include = "*" },
                tree,
                expected,
                ReferenceEquals
            );
        }

        [TestMethod]
        public void PowerShell_GetFrames_Unique()
        {
            var tree = MakeRoot(
                MakeFrame("first", ValueFactory.String("aaa"),
                    MakeFrame("second", ValueFactory.Boolean(true)),
                    MakeFrame("second", ValueFactory.Boolean(false))
                )
            );

            var expected = Flatten(tree).Take(2).ToArray();

            //Still ReferenceEquals because Get-DbgProfilerStackFrame doesn't need to rewrite the tree
            TestProfiler(
                "Get-DbgProfilerStackFrame",
                new { Unique = true },
                tree,
                expected,
                FrameEqualityComparer.Instance.Equals,
                ReferenceEquals
            );
        }

        [TestMethod]
        public void PowerShell_GetFrames_IncludeUnique_FrameStandaloneAndParentOfAnother()
        {
            var type = typeof(List<>.Enumerator);

            var moveNext = type.GetMethod("MoveNext");
            var moveNextRare = type.GetMethod("MoveNextRare", BindingFlags.Instance | BindingFlags.NonPublic);

            var tree = MakeRoot(
                MakeFrame(moveNext, null),
                MakeFrame(moveNext, null,
                    MakeFrame(moveNextRare, null)
                )
            );

            var flattened = Flatten(tree);

            var expected = new[]
            {
                flattened.First(),
                flattened.Last()
            };

            TestProfiler(
                "Get-DbgProfilerStackFrame",
                new { Include = "*movenext*", Unique = true },
                tree,
                expected,
                ReferenceEquals
            );
        }

        [TestMethod]
        public void PowerShell_GetFrames_CalledFrom()
        {
            var tree = MakeRoot(
                MakeFrame("first", ValueFactory.String("aaa"),
                    MakeFrame("second", ValueFactory.Boolean(true)),
                    MakeFrame("second", ValueFactory.Boolean(false))
                )
            );

            var expected = Flatten(tree).Skip(1).ToArray();

            TestProfiler(
                "Get-DbgProfilerStackFrame",
                new { CalledFrom = "*first*" },
                tree,
                expected,
                ReferenceEquals
            );
        }

        [TestMethod]
        public void PowerShell_GetFrames_CalledFromAll()
        {
            var tree = MakeRoot(
                MakeFrame("first", ValueFactory.String("aaa"),
                    MakeFrame("second", ValueFactory.Boolean(true)),
                    MakeFrame("second", ValueFactory.Boolean(false))
                )
            );

            var expected = Flatten(tree).ToArray();

            TestProfiler(
                "Get-DbgProfilerStackFrame",
                new { CalledFrom = "*" },
                tree,
                expected,
                ReferenceEquals
            );
        }

        [TestMethod]
        public void PowerShell_GetFrames_SortThreads()
        {
            var tree1 = MakeRoot(
                MakeFrame("first", ValueFactory.String("aaa"),
                    MakeFrame("second", ValueFactory.Boolean(true))
                ),
                1001
            );

            var tree2 = MakeRoot(
                MakeFrame("third", ValueFactory.String("aaa"),
                    MakeFrame("fourth", ValueFactory.Boolean(true))
                ),
                1002
            );

            var expected = Flatten(tree1).Union(Flatten(tree2)).ToArray();

            TestProfiler(
                "Get-DbgProfilerStackFrame",
                null,
                new[] { tree2, tree1 },
                expected,
                ReferenceEquals
            );
        }
    }
}