using System;
using System.Collections.Generic;
using System.Threading;
using DebugTools.PowerShell;
using DebugTools.Profiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Profiler.Tests
{
    [TestClass]
    public class FrameFiltererTests
    {
        [TestMethod]
        public void FrameFilterer_FilterBoolValue()
        {
            var options = new FrameFilterOptions
            {
                BoolValue = new[] {true},
                HasFilterValue = true
            };

            var tree = MakeRoot(
                MakeFrame("first", ValueFactory.String("aaa"),
                    MakeFrame("second", ValueFactory.Boolean(true)),
                    MakeFrame("second", ValueFactory.Boolean(false))
                )
            );

            TestStack(options, tree, @"
1000
└─void Methods.first(""aaa"")
  └─void Methods.second(<Yellow>true</Yellow>)
");
        }

        [TestMethod]
        public void FrameFilterer_FilterCharValue()
        {
            var options = new FrameFilterOptions
            {
                CharValue = new[] { 'b' },
                HasFilterValue = true
            };

            var tree = MakeRoot(
                MakeFrame("first", ValueFactory.String("aaa"),
                    MakeFrame("second", ValueFactory.Char('b')),
                    MakeFrame("second", ValueFactory.Char('c'))
                )
            );

            TestStack(options, tree, @"
1000
└─void Methods.first(""aaa"")
  └─void Methods.second(<Yellow>'b'</Yellow>)
");
        }

        [TestMethod]
        public void FrameFilterer_FilterSByteValue()
        {
            var options = new FrameFilterOptions
            {
                SByteValue = new sbyte[] { 0x0A },
                HasFilterValue = true
            };

            var tree = MakeRoot(
                MakeFrame("first", ValueFactory.String("aaa"),
                    MakeFrame("second", ValueFactory.SByte(0x0A)),
                    MakeFrame("second", ValueFactory.SByte(0x0B))
                )
            );

            TestStack(options, tree, @"
1000
└─void Methods.first(""aaa"")
  └─void Methods.second(<Yellow>10</Yellow>)
");
        }

        [TestMethod]
        public void FrameFilterer_FilterByteValue()
        {
            var options = new FrameFilterOptions
            {
                ByteValue = new byte[] { 0x0A },
                HasFilterValue = true
            };

            var tree = MakeRoot(
                MakeFrame("first", ValueFactory.String("aaa"),
                    MakeFrame("second", ValueFactory.Byte(0x0A)),
                    MakeFrame("second", ValueFactory.Byte(0x0B))
                )
            );

            TestStack(options, tree, @"
1000
└─void Methods.first(""aaa"")
  └─void Methods.second(<Yellow>10</Yellow>)
");
        }

        [TestMethod]
        public void FrameFilterer_FilterInt16Value()
        {
            var options = new FrameFilterOptions
            {
                Int16Value = new short[] { 100 },
                HasFilterValue = true
            };

            var tree = MakeRoot(
                MakeFrame("first", ValueFactory.String("aaa"),
                    MakeFrame("second", ValueFactory.Int16(100)),
                    MakeFrame("second", ValueFactory.Int16(101))
                )
            );

            TestStack(options, tree, @"
1000
└─void Methods.first(""aaa"")
  └─void Methods.second(<Yellow>100</Yellow>)
");
        }

        [TestMethod]
        public void FrameFilterer_FilterUInt16Value()
        {
            var options = new FrameFilterOptions
            {
                UInt16Value = new ushort[] { 100 },
                HasFilterValue = true
            };

            var tree = MakeRoot(
                MakeFrame("first", ValueFactory.String("aaa"),
                    MakeFrame("second", ValueFactory.UInt16(100)),
                    MakeFrame("second", ValueFactory.UInt16(101))
                )
            );

            TestStack(options, tree, @"
1000
└─void Methods.first(""aaa"")
  └─void Methods.second(<Yellow>100</Yellow>)
");
        }

        [TestMethod]
        public void FrameFilterer_FilterInt32Value()
        {
            var options = new FrameFilterOptions
            {
                Int32Value = new[] { 1000 },
                HasFilterValue = true
            };

            var tree = MakeRoot(
                MakeFrame("first", ValueFactory.String("aaa"),
                    MakeFrame("second", ValueFactory.Int32(1000)),
                    MakeFrame("second", ValueFactory.Int32(1001))
                )
            );

            TestStack(options, tree, @"
1000
└─void Methods.first(""aaa"")
  └─void Methods.second(<Yellow>1000</Yellow>)
");
        }

        [TestMethod]
        public void FrameFilterer_FilterUInt32Value()
        {
            var options = new FrameFilterOptions
            {
                UInt32Value = new uint[] { 1000 },
                HasFilterValue = true
            };

            var tree = MakeRoot(
                MakeFrame("first", ValueFactory.String("aaa"),
                    MakeFrame("second", ValueFactory.UInt32(1000)),
                    MakeFrame("second", ValueFactory.UInt32(1001))
                )
            );

            TestStack(options, tree, @"
1000
└─void Methods.first(""aaa"")
  └─void Methods.second(<Yellow>1000</Yellow>)
");
        }

        [TestMethod]
        public void FrameFilterer_FilterInt64Value()
        {
            var options = new FrameFilterOptions
            {
                Int64Value = new long[] { 1000 },
                HasFilterValue = true
            };

            var tree = MakeRoot(
                MakeFrame("first", ValueFactory.String("aaa"),
                    MakeFrame("second", ValueFactory.Int64(1000)),
                    MakeFrame("second", ValueFactory.Int64(1001))
                )
            );

            TestStack(options, tree, @"
1000
└─void Methods.first(""aaa"")
  └─void Methods.second(<Yellow>1000</Yellow>)
");
        }

        [TestMethod]
        public void FrameFilterer_FilterUInt64Value()
        {
            var options = new FrameFilterOptions
            {
                UInt64Value = new ulong[] { 1000 },
                HasFilterValue = true
            };

            var tree = MakeRoot(
                MakeFrame("first", ValueFactory.String("aaa"),
                    MakeFrame("second", ValueFactory.UInt64(1000)),
                    MakeFrame("second", ValueFactory.UInt64(1001))
                )
            );

            TestStack(options, tree, @"
1000
└─void Methods.first(""aaa"")
  └─void Methods.second(<Yellow>1000</Yellow>)
");
        }

        [TestMethod]
        public void FrameFilterer_FilterFloatValue()
        {
            var options = new FrameFilterOptions
            {
                FloatValue = new[] { 1.1f },
                HasFilterValue = true
            };

            var tree = MakeRoot(
                MakeFrame("first", ValueFactory.String("aaa"),
                    MakeFrame("second", ValueFactory.Float(1.1f)),
                    MakeFrame("second", ValueFactory.Float(1.2f))
                )
            );

            TestStack(options, tree, @"
1000
└─void Methods.first(""aaa"")
  └─void Methods.second(<Yellow>1.1</Yellow>)
");
        }

        [TestMethod]
        public void FrameFilterer_FilterDoubleValue()
        {
            var options = new FrameFilterOptions
            {
                DoubleValue = new[] { 1.1 },
                HasFilterValue = true
            };

            var tree = MakeRoot(
                MakeFrame("first", ValueFactory.String("aaa"),
                    MakeFrame("second", ValueFactory.Double(1.1)),
                    MakeFrame("second", ValueFactory.Double(1.2))
                )
            );

            TestStack(options, tree, @"
1000
└─void Methods.first(""aaa"")
  └─void Methods.second(<Yellow>1.1</Yellow>)
");
        }

        [TestMethod]
        public void FrameFilterer_FilterIntPtrValue()
        {
            var options = new FrameFilterOptions
            {
                IntPtrValue = new[] { new IntPtr(1000) },
                HasFilterValue = true
            };

            var tree = MakeRoot(
                MakeFrame("first", ValueFactory.String("aaa"),
                    MakeFrame("second", ValueFactory.IntPtr(new IntPtr(1000))),
                    MakeFrame("second", ValueFactory.IntPtr(new IntPtr(2000)))
                )
            );

            TestStack(options, tree, @"
1000
└─void Methods.first(""aaa"")
  └─void Methods.second(<Yellow>0x3E8</Yellow>)
");
        }

        [TestMethod]
        public void FrameFilterer_FilterUIntPtrValue()
        {
            var options = new FrameFilterOptions
            {
                UIntPtrValue = new[] { new UIntPtr(1000) },
                HasFilterValue = true
            };

            var tree = MakeRoot(
                MakeFrame("first", ValueFactory.String("aaa"),
                    MakeFrame("second", ValueFactory.UIntPtr(new UIntPtr(1000))),
                    MakeFrame("second", ValueFactory.UIntPtr(new UIntPtr(2000)))
                )
            );

            TestStack(options, tree, @"
1000
└─void Methods.first(""aaa"")
  └─void Methods.second(<Yellow>0x3E8</Yellow>)
");
        }

        [TestMethod]
        public void FrameFilterer_FilterStringValue()
        {
            var options = new FrameFilterOptions
            {
                StringValue = new[] {"bbb"},
                HasFilterValue = true
            };

            var tree = MakeRoot(
                MakeFrame("first", ValueFactory.String("aaa"),
                    MakeFrame("second", ValueFactory.String("bbb")),
                    MakeFrame("second", ValueFactory.String("ccc"))
                )
            );

            TestStack(options, tree, @"
1000
└─void Methods.first(""aaa"")
  └─void Methods.second(<Yellow>""bbb""</Yellow>)
");
        }

        private RootFrame MakeRoot(params IMethodFrame[] children)
        {
            var newFrame = new RootFrame
            {
                ThreadId = 1000,
            };

            if (children != null)
            {
                newFrame.Children.AddRange(children);

                foreach (var child in children)
                    child.Parent = newFrame;
            }

            return newFrame;
        }

        private IMethodFrameDetailed MakeFrame(string methodName, object parameter, params IMethodFrame[] children)
        {
            if (parameter is IMockValue v)
                parameter = v.OuterValue;

            var newFrame = new MockMethodFrameDetailed(
                new MockMethodInfoDetailed(typeof(Methods).GetMethod(methodName)),
                new List<object> {parameter},
                VoidValue.Instance
            );

            if (children != null)
            {
                newFrame.Children.AddRange(children);

                foreach (var child in children)
                    child.Parent = newFrame;
            }

            return newFrame;
        }

        private void TestStack(FrameFilterOptions options, IFrame tree, string expected, bool highlightFrames = false)
        {
            var output = new StringColorOutputSource();

            var filter = new FrameFilterer(options);

            filter.ProcessFrame(tree);

            var frames = filter.GetSortedMaybeValueFilteredFrames();

            var methodFrameFormatter = new MethodFrameFormatter(true);
            var methodFrameWriter = new MethodFrameColorWriter(methodFrameFormatter, output)
            {
                HighlightValues = filter.MatchedValues
            };

            if (highlightFrames)
                methodFrameWriter.HighlightFrames = filter.HighlightFrames;

            var stackWriter = new StackFrameWriter(
                methodFrameWriter,
                null,
                CancellationToken.None
            );

            stackWriter.Execute(frames);

            var actual = output.ToStringAndClear();

            Assert.AreEqual(expected.TrimStart(), actual);
        }
    }
}
