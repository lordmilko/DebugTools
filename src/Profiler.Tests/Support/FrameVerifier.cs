using System;
using System.Collections.Generic;
using System.Linq;
using ClrDebug;
using DebugTools.Profiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Profiler.Tests
{
    internal struct FrameVerifier
    {
        private IFrame frame;

        public FrameVerifier(IFrame frame)
        {
            this.frame = frame;
        }

        public FrameVerifier HasName(string name)
        {
            Assert.AreEqual(name, frame.ToString());

            return this;
        }

        public void HasFrame(string name)
        {
            var child = frame.Children.FirstOrDefault(m => m.MethodInfo.MethodName == name);

            if (child == null)
                Assert.Fail($"Failed to find child frame '{name}'.");
        }

        public void HasFrames(params string[] names)
        {
            Assert.AreEqual(names.Length, frame.Children.Count, "Expected number of child frames was not correct");

            for (var i = 0; i < names.Length; i++)
                Assert.AreEqual(names[i], frame.Children[i].MethodInfo.MethodName);
        }

        public void HasValue<T>(T value) => GetParameter().VerifyValue().HasValue(value);

        public void HasPtrValue<T>(T value) => GetParameter().VerifyValue().HasPtrValue(value);

        public void HasArrayValues<T>(CorElementType type, params T[] values)
        {
            var parameter = GetParameter();

            var arrObj = (SZArrayValue) parameter;

            Assert.AreEqual(type, arrObj.ElementType);

            Assert.AreEqual(values.Length, arrObj.Value.Length, "Expected number of array elements was incorrect");

            for (var i = 0; i < values.Length; i++)
            {
                var valueObj = (IValue<T>) arrObj.Value[i];

                Assert.AreEqual(values[i], valueObj.Value);
            }
        }

        public void HasArrayClassValues(CorElementType type, params string[] classNames)
        {
            var parameter = GetParameter();

            var arrObj = (SZArrayValue) parameter;

            Assert.AreEqual(type, arrObj.ElementType);

            Assert.AreEqual(classNames.Length, arrObj.Value.Length, "Expected number of array elements was incorrect");

            for (var i = 0; i < classNames.Length; i++)
            {
                var valueObj = (ClassValue)arrObj.Value[i];

                Assert.AreEqual(classNames[i], valueObj.Name);
            }
        }

        public void HasArrayStructValues(CorElementType type, params string[] structNames)
        {
            var parameter = GetParameter();

            var arrObj = (SZArrayValue) parameter;

            Assert.AreEqual(type, arrObj.ElementType);

            Assert.AreEqual(structNames.Length, arrObj.Value.Length, "Expected number of array elements was incorrect");

            for (var i = 0; i < structNames.Length; i++)
            {
                var valueObj = (StructValue)arrObj.Value[i];

                Assert.AreEqual(structNames[i], valueObj.Name);
            }
        }

        public ValueVerifier HasClassType(string name) => GetParameter().VerifyValue().HasClassType(name);

        public ValueVerifier HasValueType(string name) => GetParameter().VerifyValue().HasValueType(name);

        public void HasFieldValue<T>(T value) =>
            GetParameter().VerifyValue().HasFieldValue(value);

        public void HasFieldValue(Action<ValueVerifier> value) =>
            GetParameter().VerifyValue().HasFieldValue(value);

        public ValueVerifier HasFieldValue<T>(int index, T value) =>
            GetParameter().VerifyValue().HasFieldValue(index, value);

        public void VerifyArray(params Action<ValueVerifier>[] actions) =>
            GetParameter().VerifyValue().VerifyArray(actions);

        public void VerifyMultiArray(params Action<ValueVerifier>[] actions) =>
            GetParameter().VerifyValue().VerifyMultiArray(actions);

        public ValueVerifier HasPtrDisplay(string expected) =>
            GetParameter().VerifyValue().HasPtrDisplay(expected);

        public void HasError()
        {
            var parameters = GetParameters();

            Assert.IsNull(parameters);
        }

        public List<object> GetParameters()
        {
            var detailed = (IMethodFrameDetailed)frame;

            var result = detailed.GetEnterParameters();

            return result;
        }

        public object GetParameter()
        {
            var parameters = GetParameters();

            Assert.AreEqual(1, parameters.Count, "Expected number of parameters was incorrect");

            return parameters[0];
        }

        private List<object> GetFields(object parameter)
        {
            return ((ComplexTypeValue)parameter).FieldValues;
        }
    }
}
