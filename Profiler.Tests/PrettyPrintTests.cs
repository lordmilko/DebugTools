using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using DebugTools.PowerShell;
using DebugTools.Profiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Profiler.Tests.ValueFactory;

namespace Profiler.Tests
{
    delegate void SetHighlightsDelegate(MethodFrameColorWriter writer, List<object> parameters, object returnValue);

    [TestClass]
    public class PrettyPrintTests : BaseTest
    {
        [TestMethod]
        public void MethodFrameFormat_StringArg()
        {
            TestArg(
                String("test"),
                "void Methods.first(\"test\")"
            );
        }

        [TestMethod]
        public void MethodFrameFormat_ClassArg()
        {
            TestArg(
                Class("test", String("bar")),
                "void Methods.first(test)"
            );
        }

        [TestMethod]
        public void MethodFrameFormat_HighlightClassArg_StringField()
        {
            TestArg(
                () => Methods.ClassArg(new TestClass{Field1 = "bar"}),
                "void Methods.ClassArg(<Yellow>TestClass.Field1=\"bar\"</Yellow>)",
                (w, p, r) =>
                {
                    var classObj = (ClassValue) p[0];

                    w.HighlightValues[classObj] = 0;
                    w.HighlightValues[classObj.FieldValues[0]] = 0;
                }
            );
        }

        [TestMethod]
        public void MethodFrameFormat_HighlightStringArg()
        {
            TestArg(
                () => Methods.StringArg("test"),
                "void Methods.StringArg(<Yellow>\"test\"</Yellow>)",
                (w, p, r) => w.HighlightValues[p[0]] = 0
            );
        }

        private void TestArg(Expression<Action> expr, string expected, SetHighlightsDelegate setHighlights = null)
        {
            var call = (MethodCallExpression)expr.Body;
            var method = call.Method;

            var args = call.Arguments.Select(a => Expression.Lambda(a).Compile().DynamicInvoke()).ToArray();

            var objs = args.Select(a => FromRaw(a).OuterValue).ToList();

            var output = new StringColorOutputSource();

            var writer = new MethodFrameColorWriter(
                new MethodFrameFormatter(true),
                output
            );

            object returnValue = VoidValue.Instance;

            if (method.ReturnType != typeof(void))
            {
                var val = expr.Compile().DynamicInvoke();

                returnValue = ValueFactory.FromRaw(val).OuterValue;
            }

            if (setHighlights != null)
            {
                writer.HighlightValues = new ConcurrentDictionary<object, byte>();
                setHighlights.Invoke(writer, objs, returnValue);
            }

            var info = new MockMethodInfoDetailed(method);

            var frame = new MockMethodFrameDetailed(info, objs, returnValue);

            writer.Print(frame);

            Assert.AreEqual(expected, output.ToString());
        }

        private void TestArg(IMockValue parameter, string expected, SetHighlightsDelegate setHighlights = null)
        {
            Test(new List<IMockValue> {parameter}, Void, expected, setHighlights);
        }

        private void Test(List<IMockValue> parameters, object returnValue, string expected, SetHighlightsDelegate setHighlights = null)
        {
            var output = new StringColorOutputSource();

            var writer = new MethodFrameColorWriter(
                new MethodFrameFormatter(true),
                output
            );

            if (returnValue is IMockValue v)
                returnValue = v.OuterValue;

            var trueParameters = parameters.Select(p => p.OuterValue).ToList();

            if (setHighlights != null)
            {
                writer.HighlightValues = new ConcurrentDictionary<object, byte>();
                setHighlights.Invoke(writer, trueParameters, returnValue);
            }

            var info = new MockMethodInfoDetailed(typeof(Methods).GetMethod("first"));

            var frame = new MockMethodFrameDetailed(info, trueParameters, returnValue);

            writer.Print(frame);

            Assert.AreEqual(expected, output.ToString());
        }

        private VoidValue Void => VoidValue.Instance;
    }
}
