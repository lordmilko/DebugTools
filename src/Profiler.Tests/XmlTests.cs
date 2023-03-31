using System.IO;
using System.Linq;
using System.Text;
using DebugTools.Profiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Profiler.Tests.ValueFactory;

namespace Profiler.Tests
{
    [TestClass]
    public class XmlTests : BaseTest
    {
        private const string str = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Root>
  <Methods>
    <MethodInfoDetailed FunctionID=""8D83D1AC"" MethodName=""first"" TypeName=""Profiler.Tests.Methods"" ModulePath=""D:\Programming\C#\DebugTools\artifacts\bin\Debug\Profiler.Tests.dll"" Token=""600017F"" />
    <MethodInfoDetailed FunctionID=""D0BE9278"" MethodName=""second"" TypeName=""Profiler.Tests.Methods"" ModulePath=""D:\Programming\C#\DebugTools\artifacts\bin\Debug\Profiler.Tests.dll"" Token=""6000180"" />
  </Methods>
  <Frames>
    <RootFrame ThreadId=""1000"">
      <MethodFrameDetailed FunctionID=""8D83D1AC"" Sequence=""1"" Enter=""BAAAAGEAYQBhAAAA"" Leave=""BAAAAGEAYQBhAAAA"">
        <MethodFrameDetailed FunctionID=""D0BE9278"" Sequence=""2"" Enter=""AQ=="" Leave=""AQ=="" />
        <MethodFrameDetailed FunctionID=""D0BE9278"" Sequence=""3"" Enter=""AA=="" Leave=""AA=="" />
      </MethodFrameDetailed>
    </RootFrame>
    <RootFrame ThreadId=""1000"">
      <MethodFrameDetailed FunctionID=""8D83D1AC"" Sequence=""1"" Enter=""BAAAAGEAYQBhAAAA"" Leave=""BAAAAGEAYQBhAAAA"">
        <MethodFrameDetailed FunctionID=""D0BE9278"" Sequence=""2"" Enter=""AQ=="" Leave=""AQ=="" />
        <MethodFrameDetailed FunctionID=""D0BE9278"" Sequence=""3"" Enter=""AA=="" Leave=""AA=="" />
      </MethodFrameDetailed>
    </RootFrame>
  </Frames>
</Root>";

        [TestMethod]
        public void Xml_Export()
        {
            var tree = MakeRoot(
                MakeFrame("first", String("aaa"),
                    MakeFrame("second", Boolean(true)),
                    MakeFrame("second", Boolean(false))
                )
            );

            var writer = new FrameXmlWriter(tree, tree);

            using (var stream = new MemoryStream())
            {
                writer.Write(stream);

                var actual = Encoding.UTF8.GetString(stream.ToArray()).TrimStart((char)0xfeff);
                var expected = str.TrimStart();

                Assert.AreEqual(expected.Length, actual.Length);
                Assert.AreEqual(expected, actual);
            }
        }

        [TestMethod]
        public void Xml_Import()
        {
            var reader = new FrameXmlReader();

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(str)))
            {
                var result = reader.Read(stream);

                Assert.AreEqual(2, result.Length);

                foreach(var root in result)
                {
                    var rootFrame = (RootFrame)root;
                    Assert.AreEqual(1000, rootFrame.ThreadId);

                    Assert.AreEqual(1, rootFrame.Children.Count);

                    var firstChild = rootFrame.Children.Single() as IMethodFrameDetailedInternal;
                    Assert.AreEqual("Profiler.Tests.Methods.first", firstChild.MethodInfo.ToString());
                    var value1 = new StringValue(new ValueSerializer(firstChild.EnterValue).Reader);
                    Assert.AreEqual("aaa", value1.Value);

                    Assert.AreEqual(2, firstChild.Children.Count);

                    var firstGrandchild = firstChild.Children.First() as IMethodFrameDetailedInternal;
                    Assert.AreEqual("Profiler.Tests.Methods.second", firstGrandchild.MethodInfo.ToString());
                    var value2 = new BoolValue(new ValueSerializer(firstGrandchild.EnterValue).Reader);
                    Assert.AreEqual(true, value2.Value);

                    var secondGrandchild = firstChild.Children.Last() as IMethodFrameDetailedInternal;
                    Assert.AreEqual("Profiler.Tests.Methods.second", secondGrandchild.MethodInfo.ToString());
                    var value3 = new BoolValue(new ValueSerializer(secondGrandchild.EnterValue).Reader);
                    Assert.AreEqual(false, value3.Value);
                }
            }
        }
    }
}
