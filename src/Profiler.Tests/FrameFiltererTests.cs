using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using ClrDebug;
using DebugTools.PowerShell;
using DebugTools.Profiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Profiler.Tests.ValueFactory;

namespace Profiler.Tests
{
    [TestClass]
    public class FrameFiltererTests : BaseTest
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
        public void FrameFilterer_IncludeUnique()
        {
            var options = new FrameFilterOptions
            {
                Include = new[] { "*second*" },
                Unique = true
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
  └─<Green>void Methods.second(true)</Green>
");
        }

        [TestMethod]
        public void FrameFilterer_IncludeUniqueAll()
        {
            var options = new FrameFilterOptions
            {
                Include = new[] { "*" },
                Unique = true
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
  └─void Methods.second(true)
");
        }

        [TestMethod]
        public void FrameFilterer_Unique()
        {
            var options = new FrameFilterOptions
            {
                Unique = true
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
  └─void Methods.second(true)
");
        }

        [TestMethod]
        public void FrameFilterer_IncludeUnique_FrameStandaloneAndParentOfAnother()
        {
            var options = new FrameFilterOptions
            {
                Include = new[] { "*movenext*" },
                Unique = true
            };

            var type = typeof(List<>.Enumerator);

            var moveNext = type.GetMethod("MoveNext");
            var moveNextRare = type.GetMethod("MoveNextRare", BindingFlags.Instance | BindingFlags.NonPublic);

            var tree = MakeRoot(
                MakeFrame(moveNext, null),
                MakeFrame(moveNext, null,
                    MakeFrame(moveNextRare, null)
                )
            );

            TestStack(options, tree, @"
1000
└─void List`1+Enumerator.MoveNext()
  └─void List`1+Enumerator.MoveNextRare()
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
  └─<Green>void Methods.second(<Yellow>true</Yellow>)</Green>
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
  └─<Green>void Methods.second(<Yellow>'b'</Yellow>)</Green>
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
  └─<Green>void Methods.second(<Yellow>10</Yellow>)</Green>
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
  └─<Green>void Methods.second(<Yellow>10</Yellow>)</Green>
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
  └─<Green>void Methods.second(<Yellow>100</Yellow>)</Green>
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
  └─<Green>void Methods.second(<Yellow>100</Yellow>)</Green>
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
  └─<Green>void Methods.second(<Yellow>1000</Yellow>)</Green>
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
  └─<Green>void Methods.second(<Yellow>1000</Yellow>)</Green>
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
  └─<Green>void Methods.second(<Yellow>1000</Yellow>)</Green>
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
  └─<Green>void Methods.second(<Yellow>1000</Yellow>)</Green>
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
  └─<Green>void Methods.second(<Yellow>1.1</Yellow>)</Green>
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
  └─<Green>void Methods.second(<Yellow>1.1</Yellow>)</Green>
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
  └─<Green>void Methods.second(<Yellow>0x3E8</Yellow>)</Green>
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
  └─<Green>void Methods.second(<Yellow>0x3E8</Yellow>)</Green>
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
  └─<Green>void Methods.second(<Yellow>""bbb""</Yellow>)</Green>
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
  └─<Green>void Methods.second(<Yellow>foo</Yellow>)</Green>
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
  └─<Green>void Methods.second(<Yellow>foo</Yellow>)</Green>
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
  └─<Green>void Methods.second(<Yellow>new[]{""bbb"", ""ccc""}</Yellow>)</Green>
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
  └─<Green>void Methods.second(<Yellow>new[,]{{""bbb"",""ccc""},{""ddd"",""eee""}}</Yellow>)</Green>
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
  └─<Green>void Methods.second(<Yellow>char* (""bbb"")</Yellow>)</Green>
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
└─<Green>M2U Methods.second</Green>
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
└─<Green>void Methods.first(""aaa"")</Green>
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
└─<Green>void Console.WriteLine(""bbb"")</Green>
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
└─<Green>void Console.WriteLine(""bbb"")</Green>
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
  ├─<Green>void Methods.second(""bbb"")</Green>
  └─<Green>void Methods.third(""ccc"")</Green>
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
  ├─<Green>void Methods.second(""bbb"")</Green>
  └─<Green>void Methods.third(""ccc"")</Green>
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
  ├─<Green>void Methods.second(""bbb"")</Green>
  └─<Green>void Methods.third(""ccc"")</Green>
");
        }

        [TestMethod]
        public void FrameFilterer_CalledFrom()
        {
            var options = new FrameFilterOptions
            {
                CalledFrom = new[] {"second"}
            };

            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", String("bbb"),
                        MakeFrame("third", String("ccc")),
                        MakeFrame(
                            typeof(Console).GetMethods().First(m => m.Name == "ReadLine"),
                            String("ddd")
                        )
                    ),
                    MakeFrame(
                        typeof(Console).GetMethods().First(m => m.Name == "WriteLine"),
                        String("eee")
                    )
                )
            );

            TestStack(options, tree, @"
void Methods.second(""bbb"")
├─void Methods.third(""ccc"")
└─void Console.ReadLine(""ddd"")
");
        }

        [TestMethod]
        public void FrameFilterer_CalledFrom_Unique()
        {
            var options = new FrameFilterOptions
            {
                CalledFrom = new[] { "second" },
                Unique = true
            };

            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", String("bbb"),
                        MakeFrame("third", String("ccc")),
                        MakeFrame("third", String("ccc")),
                        MakeFrame(
                            typeof(Console).GetMethods().First(m => m.Name == "ReadLine"),
                            String("ddd")
                        )
                    ),
                    MakeFrame(
                        typeof(Console).GetMethods().First(m => m.Name == "WriteLine"),
                        String("eee")
                    )
                )
            );

            TestStack(options, tree, @"
1000
└─void Methods.second(""bbb"")
  ├─<Green>void Methods.third(""ccc"")</Green>
  └─<Green>void Console.ReadLine(""ddd"")</Green>
");
        }

        [TestMethod]
        public void FrameFilterer_CalledFrom_TwiceInStack()
        {
            var options = new FrameFilterOptions
            {
                CalledFrom = new[] { "second" }
            };

            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", String("bbb"),
                        MakeFrame("third", String("ccc")),
                        MakeFrame("third", String("ddd"),
                            MakeFrame("second", String("eee"))
                        ),
                        MakeFrame(
                            typeof(Console).GetMethods().First(m => m.Name == "ReadLine"),
                            String("fff")
                        )
                    ),
                    MakeFrame(
                        typeof(Console).GetMethods().First(m => m.Name == "WriteLine"),
                        String("ggg")
                    )
                )
            );

            TestStack(options, tree, @"
void Methods.second(""bbb"")
├─void Methods.third(""ccc"")
├─void Methods.third(""ddd"")
│ └─void Methods.second(""eee"")
└─void Console.ReadLine(""fff"")
");
        }

        [TestMethod]
        public void FrameFilterer_CalledFrom_Include()
        {
            var options = new FrameFilterOptions
            {
                CalledFrom = new[] { "second" },
                Include = new[] {"fourth"}
            };

            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", String("bbb"),
                        MakeFrame("third", String("ccc")),
                        MakeFrame("third", String("ddd"),
                            MakeFrame("fourth", String("eee")),
                            MakeFrame("first", String("fff"))
                        ),
                        MakeFrame(
                            typeof(Console).GetMethods().First(m => m.Name == "ReadLine"),
                            String("fff")
                        )
                    ),
                    MakeFrame(
                        typeof(Console).GetMethods().First(m => m.Name == "WriteLine"),
                        String("ggg")
                    )
                )
            );

            TestStack(options, tree, @"
1000
└─void Methods.second(""bbb"")
  └─void Methods.third(""ddd"")
    └─<Green>void Methods.fourth(""eee"")</Green>
");
        }

        [TestMethod]
        public void FrameFilterer_CalledFrom_All()
        {
            var options = new FrameFilterOptions
            {
                CalledFrom = new[] { "*" },
            };

            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", String("bbb"),
                        MakeFrame("third", String("ccc")),
                        MakeFrame("third", String("ddd"),
                            MakeFrame("fourth", String("eee")),
                            MakeFrame("first", String("fff"))
                        ),
                        MakeFrame(
                            typeof(Console).GetMethods().First(m => m.Name == "ReadLine"),
                            String("fff")
                        )
                    ),
                    MakeFrame(
                        typeof(Console).GetMethods().First(m => m.Name == "WriteLine"),
                        String("ggg")
                    )
                )
            );

            TestStack(options, tree, @"
1000
└─void Methods.first(""aaa"")
  ├─void Methods.second(""bbb"")
  │ ├─void Methods.third(""ccc"")
  │ ├─void Methods.third(""ddd"")
  │ │ ├─void Methods.fourth(""eee"")
  │ │ └─void Methods.first(""fff"")
  │ └─void Console.ReadLine(""fff"")
  └─void Console.WriteLine(""ggg"")
");
        }

        [TestMethod]
        public void FrameFilterer_CalledFrom_AllUnique()
        {
            var options = new FrameFilterOptions
            {
                CalledFrom = new[] { "*" },
                Unique = true
            };

            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", String("bbb"),
                        MakeFrame("third", String("ccc")),
                        MakeFrame("third", String("ddd"),
                            MakeFrame("fourth", String("eee")),
                            MakeFrame("first", String("fff"))
                        ),
                        MakeFrame(
                            typeof(Console).GetMethods().First(m => m.Name == "ReadLine"),
                            String("fff")
                        )
                    ),
                    MakeFrame(
                        typeof(Console).GetMethods().First(m => m.Name == "WriteLine"),
                        String("ggg")
                    )
                )
            );

            TestStack(options, tree, @"
1000
└─void Methods.first(""aaa"")
  ├─void Methods.second(""bbb"")
  │ ├─void Methods.third(""ccc"")
  │ └─void Console.ReadLine(""fff"")
  └─void Console.WriteLine(""ggg"")
");
        }

        [TestMethod]
        public void FrameFilterer_CalledFrom_Exclude()
        {
            var options = new FrameFilterOptions
            {
                CalledFrom = new[] { "second" },
                Exclude = new[] { "fourth" }
            };

            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", String("bbb"),
                        MakeFrame("third", String("ccc")),
                        MakeFrame("third", String("ddd"),
                            MakeFrame("fourth", String("eee")),
                            MakeFrame("first", String("fff"))
                        ),
                        MakeFrame(
                            typeof(Console).GetMethods().First(m => m.Name == "ReadLine"),
                            String("fff")
                        )
                    ),
                    MakeFrame(
                        typeof(Console).GetMethods().First(m => m.Name == "WriteLine"),
                        String("ggg")
                    )
                )
            );

            TestStack(options, tree, @"
1000
└─void Methods.second(""bbb"")
  ├─<Green>void Methods.third(""ccc"")</Green>
  ├─<Green>void Methods.third(""ddd"")</Green>
  │ └─<Green>void Methods.first(""fff"")</Green>
  └─<Green>void Console.ReadLine(""fff"")</Green>
");
        }

        [TestMethod]
        public void FrameFilterer_CalledFrom_Include_Unique()
        {
            var options = new FrameFilterOptions
            {
                CalledFrom = new[] { "second" },
                Include = new[] { "fourth" },
                Unique = true
            };

            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", String("bbb"),
                        MakeFrame("first", String("hhh"),
                            MakeFrame("third", String("ccc")),
                            MakeFrame("third", String("ddd"),
                                MakeFrame("fourth", String("eee")),
                                MakeFrame("first", String("fff"))
                            ),
                            MakeFrame(
                                typeof(Console).GetMethods().First(m => m.Name == "ReadLine"),
                                String("fff")
                            )
                        )
                    ),
                    MakeFrame(
                        typeof(Console).GetMethods().First(m => m.Name == "WriteLine"),
                        String("ggg")
                    )
                )
            );

            TestStack(options, tree, @"
1000
└─void Methods.second(""bbb"")
  └─void Methods.first(""hhh"")
    └─void Methods.third(""ddd"")
      └─<Green>void Methods.fourth(""eee"")</Green>
");
        }

        [TestMethod]
        public void FrameFilterer_CalledFrom_Exclude_Unique()
        {
            //todo: randomly fails when third(ddd) is selected

            var options = new FrameFilterOptions
            {
                CalledFrom = new[] { "second" },
                Exclude = new[] { "fourth" },
                Unique = true
            };

            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", String("bbb"),
                        MakeFrame("third", String("ccc")),
                        MakeFrame("third", String("ddd"),
                            MakeFrame("fourth", String("eee")),
                            MakeFrame("first", String("fff"))
                        ),
                        MakeFrame(
                            typeof(Console).GetMethods().First(m => m.Name == "ReadLine"),
                            String("fff")
                        )
                    ),
                    MakeFrame(
                        typeof(Console).GetMethods().First(m => m.Name == "WriteLine"),
                        String("ggg")
                    )
                )
            );

            TestStack(options, tree, @"
1000
└─void Methods.second(""bbb"")
  ├─<Green>void Methods.third(""ccc"")</Green>
  └─<Green>void Console.ReadLine(""fff"")</Green>
");
        }

        [TestMethod]
        public void FrameFilterer_SortThreads()
        {
            var options = new FrameFilterOptions();
            var filter = new FrameFilterer(options);

            var tree1 = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", Boolean(true))
                ),
                1001
            );

            var tree2 = MakeRoot(
                MakeFrame("third", String("aaa"),
                    MakeFrame("fourth", Boolean(true))
                ),
                1002
            );

            filter.ProcessFrame(tree2);
            filter.ProcessFrame(tree1);

            var results = filter.GetSortedFilteredFrameRoots();

            Assert.AreEqual(2, results.Count);
            Assert.AreEqual(tree1, results[0]);
            Assert.AreEqual(tree2, results[1]);
        }

        [TestMethod]
        public void FrameFilterer_Deterministic_NoArgs()
        {
            TestDeterministic(new FrameFilterOptions());
        }

        [TestMethod]
        public void FrameFilterer_DeterministicRoot_NoArgs()
        {
            TestDeterministicRoot(new FrameFilterOptions());
        }

        [TestMethod]
        public void FrameFilterer_Deterministic_Unique()
        {
            TestDeterministic(new FrameFilterOptions
            {
                Unique = true
            });
        }

        [TestMethod]
        public void FrameFilterer_DeterministicRoot_Unique()
        {
            //this is randomly failing even with 100 iterations

            TestDeterministicRoot(new FrameFilterOptions
            {
                Unique = true
            });
        }

        [TestMethod]
        public void FrameFilterer_Deterministic_IncludeUnique()
        {
            TestDeterministic(new FrameFilterOptions
            {
                Include = new[] {"*"},
                Unique = true
            });
        }

        [TestMethod]
        public void FrameFilterer_DeterministicRoot_IncludeUnique()
        {
            TestDeterministicRoot(new FrameFilterOptions
            {
                Include = new[] { "*" },
                Unique = true
            });
        }

        [TestMethod]
        public void FrameFilterer_Deterministic_CalledFromUnique()
        {
            TestDeterministic(new FrameFilterOptions
            {
                CalledFrom = new[] {"*"},
                Unique = true
            });
        }

        [TestMethod]
        public void FrameFilterer_DeterministicRoot_CalledFromUnique()
        {
            TestDeterministicRoot(new FrameFilterOptions
            {
                CalledFrom = new[] { "*" },
                Unique = true
            });
        }

        private void TestDeterministic(FrameFilterOptions options) =>
            TestDeterministicInternal(options, f => f.GetSortedFilteredFrames());

        private void TestDeterministicRoot(FrameFilterOptions options)
        {
            TestDeterministicInternal(options, f =>
            {
                var roots = f.GetSortedFilteredFrameRoots();

                return roots.SelectMany(v => Flatten(v, true));
            });
        }

        private void TestDeterministicInternal(FrameFilterOptions options, Func<FrameFilterer, IEnumerable<IFrame>> getFrames)
        {
            var settings = new ProfilerSetting[]
            {
                ProfilerSetting.IgnoreDefaultBlacklist
            };

            TestInternal(TestType.Profiler, ProfilerTestType.Async.ToString(), v =>
            {
                IFrame[] first = null;

                for (var i = 0; i < 10; i++)
                {
                    var filterer = new FrameFilterer(options);

                    foreach (var thread in v.ThreadStacks)
                        filterer.ProcessFrame(thread.Root);

                    var result = filterer.GetSortedFilteredFrames().ToArray();

                    if (i == 0)
                        first = result;
                    else
                    {
                        Assert.AreEqual(first.Length, result.Length);

                        for (var j = 0; j < first.Length; j++)
                        {
                            var a = first[j];
                            var b = result[j];

                            var isEqual = FrameEqualityComparer.Instance.Equals(a, b);

                            if (!isEqual && a.Sequence == b.Sequence)
                            {
                                //The sequences are the same, the issue is just that we don't have the ability to sort by sequence and then thread. Get all adjacent frames of the same sequence, and check that one of them is the match

                                var sameSequence = new List<IFrame>();

                                for (var k = j - 1; k >= 0; k--)
                                {
                                    if (result[k].Sequence == b.Sequence)
                                        sameSequence.Add(result[k]);
                                    else
                                        break;
                                }

                                for (var k = j + 1; k < first.Length; k++)
                                {
                                    if (result[k].Sequence == b.Sequence)
                                        sameSequence.Add(result[k]);
                                    else
                                        break;
                                }

                                if (!sameSequence.Any(s => FrameEqualityComparer.Instance.Equals(a, s)))
                                    Assert.Fail($"Frame {j} was not equal on iteration {i}");
                            }
                        }
                    }
                }
            }, settings);
        }

        private void TestStack(FrameFilterOptions options, IFrame tree, string expected)
        {
            var output = new StringColorOutputSource();

            var filter = new FrameFilterer(options);

            filter.ProcessFrame(tree);

            var frames = filter.GetSortedFilteredFrameRoots();

            var methodFrameFormatter = new MethodFrameFormatter(true);
            var methodFrameWriter = new MethodFrameColorWriter(methodFrameFormatter, output, KnownModules)
            {
                HighlightValues = filter.MatchedValues,
                HighlightFrames = filter.HighlightFrames
            };

            var stackWriter = new StackFrameWriter(
                methodFrameWriter,
                null,
                CancellationToken.None
            );

            stackWriter.Execute(frames);

            var actual = output.ToStringAndClear();

            //Expected string literal will be in a document with a specific CRLF format. Normalize to be Environment.NewLine
            expected = string.Join(Environment.NewLine, expected.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)) + Environment.NewLine;

            expected = expected.TrimStart().Replace("\n", "\\n").Replace("\r", "\\r");
            actual = actual.Replace("\n", "\\n").Replace("\r", "\\r"); ;

            Assert.AreEqual(expected, actual);
        }
    }
}
