using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ClrDebug;
using DebugTools.PowerShell;
using DebugTools.Profiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Profiler.Tests.ValueFactory;

namespace Profiler.Tests
{
    [TestClass]
    public class FrameFiltererTests
    {
        [TestMethod]
        public void FrameFilterer_Include()
        {
            var options = new FrameFilterOptions
            {
                Include = new[] {"*"}
            };

            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", Boolean(true)),
                    MakeFrame("second", Boolean(false))
                )
            );

            TestStack(options, tree, @"
1000
└─void Methods.first(""aaa"")
  ├─void Methods.second(true)
  └─void Methods.second(false)
");
        }

        [TestMethod]
        public void FrameFilterer_FilterBoolValue()
        {
            var options = new FrameFilterOptions
            {
                BoolValue = new[] {true}
            };

            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", Boolean(true)),
                    MakeFrame("second", Boolean(false))
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
                CharValue = new[] { 'b' }
            };

            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", Char('b')),
                    MakeFrame("second", Char('c'))
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
                SByteValue = new sbyte[] { 0x0A }
            };

            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", SByte(0x0A)),
                    MakeFrame("second", SByte(0x0B))
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
                ByteValue = new byte[] { 0x0A }
            };

            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", Byte(0x0A)),
                    MakeFrame("second", Byte(0x0B))
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
                Int16Value = new short[] { 100 }
            };

            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", Int16(100)),
                    MakeFrame("second", Int16(101))
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
                UInt16Value = new ushort[] { 100 }
            };

            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", UInt16(100)),
                    MakeFrame("second", UInt16(101))
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
                Int32Value = new[] { 1000 }
            };

            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", Int32(1000)),
                    MakeFrame("second", Int32(1001))
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
                UInt32Value = new uint[] { 1000 }
            };

            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", UInt32(1000)),
                    MakeFrame("second", UInt32(1001))
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
                Int64Value = new long[] { 1000 }
            };

            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", Int64(1000)),
                    MakeFrame("second", Int64(1001))
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
                UInt64Value = new ulong[] { 1000 }
            };

            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", UInt64(1000)),
                    MakeFrame("second", UInt64(1001))
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
                FloatValue = new[] { 1.1f }
            };

            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", Float(1.1f)),
                    MakeFrame("second", Float(1.2f))
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
                DoubleValue = new[] { 1.1 }
            };

            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", Double(1.1)),
                    MakeFrame("second", Double(1.2))
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
                IntPtrValue = new[] { new IntPtr(1000) }
            };

            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", IntPtr(new IntPtr(1000))),
                    MakeFrame("second", IntPtr(new IntPtr(2000)))
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
                UIntPtrValue = new[] { new UIntPtr(1000) }
            };

            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", UIntPtr(new UIntPtr(1000))),
                    MakeFrame("second", UIntPtr(new UIntPtr(2000)))
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
                StringValue = new[] {"bbb"}
            };

            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", String("bbb")),
                    MakeFrame("second", String("ccc"))
                )
            );

            TestStack(options, tree, @"
1000
└─void Methods.first(""aaa"")
  └─void Methods.second(<Yellow>""bbb""</Yellow>)
");
        }

        [TestMethod]
        public void FrameFilterer_FilterClassField()
        {
            var options = new FrameFilterOptions
            {
                StringValue = new[] { "bbb" }
            };

            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", Class("foo", String("bbb"))),
                    MakeFrame("second", Class("foo", String("ccc")))
                )
            );

            TestStack(options, tree, @"
1000
└─void Methods.first(""aaa"")
  └─void Methods.second(<Yellow>foo</Yellow>)
");
        }

        [TestMethod]
        public void FrameFilterer_FilterStructField()
        {
            var options = new FrameFilterOptions
            {
                StringValue = new[] { "bbb" }
            };

            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", Struct("foo", String("bbb"))),
                    MakeFrame("second", Struct("foo", String("ccc")))
                )
            );

            TestStack(options, tree, @"
1000
└─void Methods.first(""aaa"")
  └─void Methods.second(<Yellow>foo</Yellow>)
");
        }

        [TestMethod]
        public void FrameFilterer_FilterSZArrayElement()
        {
            var options = new FrameFilterOptions
            {
                StringValue = new[] { "bbb" }
            };

            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", SZArray(
                        CorElementType.String,
                        String("bbb"),
                        String("ccc")
                    )),
                    MakeFrame("second", SZArray(
                        CorElementType.String,
                        String("ddd"),
                        String("eee")
                    ))
                )
            );

            TestStack(options, tree, @"
1000
└─void Methods.first(""aaa"")
  └─void Methods.second(<Yellow>new[]{""bbb"", ""ccc""}</Yellow>)
");
        }

        [TestMethod]
        public void FrameFilterer_FilterArrayElement()
        {
            var options = new FrameFilterOptions
            {
                StringValue = new[] { "bbb" }
            };

            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", Array(
                        CorElementType.String,
                        new[,] { { String("bbb"), String("ccc") }, { String("ddd"), String("eee") } }
                    )),
                    MakeFrame("second", Array(
                        CorElementType.String,
                        new[,] { { String("fff"), String("ggg") }, { String("hhh"), String("iii") } }
                    ))
                )
            );

            TestStack(options, tree, @"
1000
└─void Methods.first(""aaa"")
  └─void Methods.second(<Yellow>new[,]{{""bbb"",""ccc""},{""ddd"",""eee""}}</Yellow>)
");
        }

        [TestMethod]
        public void FrameFilterer_FilterPointerValue()
        {
            var options = new FrameFilterOptions
            {
                StringValue = new[] { "bbb" }
            };

            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", Ptr(String("bbb"))),
                    MakeFrame("second", Ptr(String("ccc")))
                )
            );

            TestStack(options, tree, @"
1000
└─void Methods.first(""aaa"")
  └─void Methods.second(<Yellow>char* (""bbb"")</Yellow>)
");
        }

        [TestMethod]
        public void FrameFilterer_FilterUnmanaged()
        {
            var options = new FrameFilterOptions
            {
                Unmanaged = true
            };

            var tree = MakeRoot(
                MakeFrame("first", String("aaa")),
                MakeUnmanagedFrame("second")
            );

            TestStack(options, tree, @"
1000
└─M2U Methods.second
");
        }

        [TestMethod]
        public void FrameFilterer_MethodModuleName()
        {
            var options = new FrameFilterOptions
            {
                MethodModuleName = new[] {"Profiler.Tests.dll"}
            };

            var tree = MakeRoot(
                MakeFrame("first", String("aaa")),
                MakeFrame(typeof(Console).GetMethods().First(m => m.Name == "WriteLine"), String("bbb"))
            );

            TestStack(options, tree, @"
1000
└─void Methods.first(""aaa"")
");
        }

        [TestMethod]
        public void FrameFilterer_MethodTypeName()
        {
            var options = new FrameFilterOptions
            {
                MethodTypeName = new[] { "System.Console" }
            };

            var tree = MakeRoot(
                MakeFrame("first", String("aaa")),
                MakeFrame(typeof(Console).GetMethods().First(m => m.Name == "WriteLine"), String("bbb"))
            );

            TestStack(options, tree, @"
1000
└─void Console.WriteLine(""bbb"")
");
        }

        [TestMethod]
        public void FrameFilterer_MethodName()
        {
            var options = new FrameFilterOptions
            {
                MethodName = new[] { "WriteLine" }
            };

            var tree = MakeRoot(
                MakeFrame("first", String("aaa")),
                MakeFrame(typeof(Console).GetMethods().First(m => m.Name == "WriteLine"), String("bbb"))
            );

            TestStack(options, tree, @"
1000
└─void Console.WriteLine(""bbb"")
");
        }

        [TestMethod]
        public void FrameFilterer_ParentMethodModuleName()
        {
            var options = new FrameFilterOptions
            {
                ParentMethodModuleName = new[] { "Profiler.Tests.dll" }
            };

            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", String("bbb")),
                    MakeFrame("third", String("ccc"))
                ),
                MakeFrame(typeof(Console).GetMethods().First(m => m.Name == "WriteLine"), String("bbb"),
                    MakeFrame("second", String("bbb")),
                    MakeFrame("third", String("ccc"))
                )
            );

            TestStack(options, tree, @"
1000
└─void Methods.first(""aaa"")
  ├─void Methods.second(""bbb"")
  └─void Methods.third(""ccc"")
");
        }

        [TestMethod]
        public void FrameFilterer_ParentMethodTypeName()
        {
            var options = new FrameFilterOptions
            {
                ParentMethodTypeName = new[] { "System.Console" }
            };

            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", String("bbb")),
                    MakeFrame("third", String("ccc"))
                ),
                MakeFrame(typeof(Console).GetMethods().First(m => m.Name == "WriteLine"), String("bbb"),
                    MakeFrame("second", String("bbb")),
                    MakeFrame("third", String("ccc"))
                )
            );

            TestStack(options, tree, @"
1000
└─void Console.WriteLine(""bbb"")
  ├─void Methods.second(""bbb"")
  └─void Methods.third(""ccc"")
");
        }

        [TestMethod]
        public void FrameFilterer_ParentMethodName()
        {
            var options = new FrameFilterOptions
            {
                ParentMethodName = new[] { "WriteLine" }
            };

            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", String("bbb")),
                    MakeFrame("third", String("ccc"))
                ),
                MakeFrame(typeof(Console).GetMethods().First(m => m.Name == "WriteLine"), String("bbb"),
                    MakeFrame("second", String("bbb")),
                    MakeFrame("third", String("ccc"))
                )
            );

            TestStack(options, tree, @"
1000
└─void Console.WriteLine(""bbb"")
  ├─void Methods.second(""bbb"")
  └─void Methods.third(""ccc"")
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

        private IMethodFrameDetailed MakeFrame(string methodName, object parameter, params IMethodFrame[] children) =>
            MakeFrame(typeof(Methods).GetMethod(methodName), parameter, children);

        private IMethodFrameDetailed MakeFrame(System.Reflection.MethodInfo method, object parameter, params IMethodFrame[] children)
        {
            if (parameter is IMockValue v)
                parameter = v.OuterValue;

            var newFrame = new MockMethodFrameDetailed(
                new MockMethodInfoDetailed(method),
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

        private IUnmanagedTransitionFrame MakeUnmanagedFrame(string methodName, params IMethodFrame[] children)
        {
            var newFrame = new MockUnmanagedTransitionFrame(
                new MockMethodInfo(typeof(Methods).GetMethod(methodName)),
                FrameKind.M2U
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

            var frames = filter.GetSortedFilteredFrames();

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
