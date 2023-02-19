using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using ClrDebug;
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
        public void MethodFrameFormat_SZArrayArg()
        {
            TestArg(
                SZArray(CorElementType.String, String("first"), String("second")),
                "void Methods.first(new[]{\"first\", \"second\"})"
            );
        }

        [TestMethod]
        public void MethodFrameFormat_ArrayArg()
        {
            TestArg(
                Array(CorElementType.I4, new IMockValue[2,3,4]
                {
                    {
                        { Int32(1), Int32(2), Int32(3), Int32(4) },
                        { Int32(5), Int32(6), Int32(7), Int32(8) },
                        { Int32(9), Int32(10), Int32(11), Int32(12) }
                    },
                    {
                        { Int32(13), Int32(14), Int32(15), Int32(16) },
                        { Int32(17), Int32(18), Int32(19), Int32(20) },
                        { Int32(21), Int32(22), Int32(23), Int32(24) }
                    }
                }),
                "void Methods.first(new[,,]{{{1,2,3,4},{5,6,7,8},{9,10,11,12}},{{13,14,15,16},{17,18,19,20},{21,22,23,24}}})"
            );
        }

        [TestMethod]
        public void MethodFrameFormat_PointerArg()
        {
            TestArg(
                Ptr(String("first")),
                "void Methods.first(char* (\"first\"))"
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
        public void MethodFrameFormat_HighlightClassArg_ClassField()
        {
            TestArg(
                () => Methods.ClassWithClassFieldArg(new TestClassWithClassField { Field1 = new TestClass{Field1 = "b"} }),
                "void Methods.ClassWithClassFieldArg(<Yellow>TestClassWithClassField.Field1</Yellow>)",
                (w, p, r) =>
                {
                    var classObj = (ClassValue)p[0];

                    w.HighlightValues[classObj] = 0;
                    w.HighlightValues[classObj.FieldValues[0]] = 0;
                }
            );
        }

        [TestMethod]
        public void MethodFrameFormat_HighlightSZArrayArg()
        {
            TestArg(
                () => Methods.SZArrayArg(new[]{"first", "second"}),
                "void Methods.SZArrayArg(<Yellow>new[]{\"first\", \"second\"}</Yellow>)",
                (w, p, r) =>
                {
                    var arrObj = (SZArrayValue) p[0];

                    w.HighlightValues[arrObj] = 0;
                    w.HighlightValues[arrObj.Value[1]] = 0;
                }
            );
        }

        [TestMethod]
        public void MethodFrameFormat_HighlightSZArrayField()
        {
            TestArg(
                () => Methods.SZArrayField(new TestClassWithSZArrayField { Field1 = new[] { "first", "second" } }),
                "void Methods.SZArrayField(<Yellow>TestClassWithSZArrayField.Field1[1]=\"second\"</Yellow>)",
                (w, p, r) =>
                {
                    var classObj = (ClassValue)p[0];
                    var arrObj = (SZArrayValue)classObj.FieldValues[0];
                    var elm = arrObj.Value[1];

                    w.HighlightValues[classObj] = 0;
                    w.HighlightValues[arrObj] = 0;
                    w.HighlightValues[elm] = 0;
                }
            );
        }

        [TestMethod]
        public void MethodFrameFormat_HighlightArrayArg()
        {
            var arr = new object[,]
            {
                {"first", "second"},
                {"third", "fourth"}
            };

            TestArg(
                () => Methods.ArrayArg(arr),
                "void Methods.ArrayArg(<Yellow>new[,]{{\"first\",\"second\"},{\"third\",\"fourth\"}}</Yellow>)",
                (w, p, r) =>
                {
                    var arrObj = (ArrayValue) p[0];

                    w.HighlightValues[arrObj] = 0;
                    w.HighlightValues[arrObj.Value.GetValue(0, 0)] = 0;
                }
            );
        }

        [TestMethod]
        public void MethodFrameFormat_HighlightArrayField()
        {
            var arr = new[,]
            {
                {"first", "second"},
                {"third", "fourth"}
            };

            TestArg(
                () => Methods.ArrayField(new TestClassWithArrayField { Field1 = arr }),
                "void Methods.ArrayField(<Yellow>TestClassWithArrayField.Field1[1,1]=\"fourth\"</Yellow>)",
                (w, p, r) =>
                {
                    var classObj = (ClassValue)p[0];
                    var arrObj = (ArrayValue)classObj.FieldValues[0];
                    var elm = arrObj.Value.GetValue(1, 1);

                    w.HighlightValues[classObj] = 0;
                    w.HighlightValues[arrObj] = 0;
                    w.HighlightValues[elm] = 0;
                }
            );
        }

        [TestMethod]
        public void MethodFrameFormat_HighlightPointerSimpleArg()
        {
            TestArg(
                Ptr(String("foo")),
                "void Methods.PointerSimpleArg(<Yellow>char* (\"foo\")</Yellow>)",
                (w, p, r) =>
                {
                    var ptr = (PtrValue)p[0];

                    w.HighlightValues[ptr] = 0;
                    w.HighlightValues[ptr.Value] = 0;
                },
                "PointerSimpleArg"
            );
        }

        [TestMethod]
        public void MethodFrameFormat_HighlightPointerStructArg()
        {
            TestArg(
                Ptr(
                    Struct("TestStructWithSimpleField",
                        Int32(5)
                    )
                ),
                "void Methods.PointerStructArg(<Yellow>TestStructWithSimpleField*</Yellow>)",
                (w, p, r) =>
                {
                    var ptr = (PtrValue)p[0];

                    w.HighlightValues[ptr] = 0;
                    w.HighlightValues[ptr.Value] = 0;
                },
                "PointerStructArg"
            );
        }

        [TestMethod]
        public void MethodFrameFormat_HighlightPointerStructWithFieldArg()
        {
            TestArg(
                Ptr(
                    Struct("TestStructWithSimpleField",
                        Int32(5)
                    )
                ),
                "void Methods.PointerStructArg(<Yellow>TestStructWithSimpleField*->Field2=5</Yellow>)",
                (w, p, r) =>
                {
                    var ptr = (PtrValue)p[0];
                    var @struct = (StructValue) ptr.Value;

                    w.HighlightValues[ptr] = 0;
                    w.HighlightValues[@struct] = 0;
                    w.HighlightValues[@struct.FieldValues[0]] = 0;
                },
                "PointerStructArg"
            );
        }

        [TestMethod]
        public void MethodFrameFormat_HighlightPointerSimpleField()
        {
            TestArg(
                Class("TestClassWithPointerSimpleField",
                    Ptr(String("foo"))
                ),
                "void Methods.PointerSimpleField(<Yellow>TestClassWithPointerSimpleField.Field1=\"foo\"</Yellow>)",
                (w, p, r) =>
                {
                    var classObj = (ClassValue) p[0];

                    var ptr = (PtrValue)classObj.FieldValues[0];

                    w.HighlightValues[classObj] = 0;
                    w.HighlightValues[ptr] = 0;
                    w.HighlightValues[ptr.Value] = 0;
                },
                "PointerSimpleField"
            );
        }

        [TestMethod]
        public void MethodFrameFormat_HighlightPointerPointerSimpleField()
        {
            TestArg(
                Class("TestClassWithPointerPointerSimpleField",
                    Ptr(Ptr(String("foo")))
                ),
                "void Methods.PointerPointerSimpleField(<Yellow>TestClassWithPointerPointerSimpleField.Field1=\"foo\"</Yellow>)",
                (w, p, r) =>
                {
                    var classObj = (ClassValue)p[0];

                    var ptrPtr = (PtrValue)classObj.FieldValues[0];
                    var ptr = (PtrValue)ptrPtr.Value;

                    w.HighlightValues[classObj] = 0;
                    w.HighlightValues[ptrPtr] = 0;
                    w.HighlightValues[ptr] = 0;
                    w.HighlightValues[ptr.Value] = 0;
                },
                "PointerPointerSimpleField"
            );
        }

        [TestMethod]
        public void MethodFrameFormat_HighlightPointerStructField()
        {
            //We matched the typename of a struct inside a pointer

            TestArg(
                Class("TestClassWithPointerStructField",
                    Ptr(
                        Struct("TestStructWithSimpleField",
                            Int32(5)
                        )
                    )
                ),
                "void Methods.PointerStructField(<Yellow>TestClassWithPointerStructField.Field1</Yellow>)",
                (w, p, r) =>
                {
                    var classObj = (ClassValue)p[0];

                    var ptr = (PtrValue)classObj.FieldValues[0];

                    w.HighlightValues[classObj] = 0;
                    w.HighlightValues[ptr] = 0;
                    w.HighlightValues[ptr.Value] = 0;
                },
                "PointerStructField"
            );
        }

        [TestMethod]
        public void MethodFrameFormat_HighlightPointerStructWithSimpleField()
        {
            TestArg(
                Class("TestClassWithPointerStructField",
                    Ptr(
                        Struct("TestStructWithSimpleField",
                            Int32(5)
                        )
                    )
                ),
                "void Methods.PointerStructField(<Yellow>TestClassWithPointerStructField.Field1->Field2=5</Yellow>)",
                (w, p, r) =>
                {
                    var classObj = (ClassValue)p[0];

                    var ptr = (PtrValue)classObj.FieldValues[0];
                    var @struct = (StructValue) ptr.Value;

                    w.HighlightValues[classObj] = 0;
                    w.HighlightValues[ptr] = 0;
                    w.HighlightValues[@struct] = 0;
                    w.HighlightValues[@struct.FieldValues[0]] = 0;
                },
                "PointerStructField"
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

                returnValue = FromRaw(val).OuterValue;
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

        private void TestArg(IMockValue parameter, string expected, SetHighlightsDelegate setHighlights = null, string methodName = "first")
        {
            Test(new List<IMockValue> {parameter}, Void, expected, setHighlights, methodName);
        }

        private void Test(List<IMockValue> parameters, object returnValue, string expected, SetHighlightsDelegate setHighlights = null, string methodName = "first")
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

            var info = new MockMethodInfoDetailed(typeof(Methods).GetMethod(methodName));

            var frame = new MockMethodFrameDetailed(info, trueParameters, returnValue);

            writer.Print(frame);

            Assert.AreEqual(expected, output.ToString());
        }

        private VoidValue Void => VoidValue.Instance;
    }
}
