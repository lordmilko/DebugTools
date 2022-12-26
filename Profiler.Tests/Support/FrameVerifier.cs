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

        public void HasValue<T>(T value)
        {
            var parameter = GetParameter();

            var valueObj = (IValue<T>) parameter;

            Assert.AreEqual(value, valueObj.Value);
        }

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
                var valueObj = (ValueType)arrObj.Value[i];

                Assert.AreEqual(structNames[i], valueObj.Name);
            }
        }

        public void HasClassType(string name)
        {
            var parameter = GetParameter();

            var classObj = (ClassValue) parameter;

            Assert.AreEqual(name, classObj.Name);
        }

        public void HasValueType(string name)
        {
            var parameter = GetParameter();

            var valueObj = (ValueType) parameter;

            Assert.AreEqual(name, valueObj.Name);
        }

        public void HasFieldValue<T>(T value)
        {
            var parameter = GetParameter();
            var fields = GetFields(parameter);

            Assert.AreEqual(1, fields.Count, "Expected number of fields was incorrect");

            var valueObj = (IValue<T>) fields[0];

            Assert.AreEqual(value, valueObj.Value);
        }

        public void HasFieldValue<T>(int index, T value)
        {
            var parameter = GetParameter();

            var fields = GetFields(parameter);

            if (fields.Count < index)
                Assert.Fail($"Expected fields to have at least {index + 1} element(s). Actual: {fields.Count}");

            var valueObj = (IValue<T>)fields[index];

            Assert.AreEqual(value, valueObj.Value);
        }

        public List<object> GetParameters()
        {
            var detailed = (MethodFrameDetailed)frame;

            var serializer = new ValueSerializer(detailed.Value);

            var parameters = serializer.Parameters;

            return parameters;
        }

        public object GetParameter()
        {
            var parameters = GetParameters();

            Assert.AreEqual(1, parameters.Count, "Expected number of parameters was incorrect");

            return parameters[0];
        }

        private List<object> GetFields(object parameter)
        {
            List<object> fields;

            if (parameter is ClassValue)
                fields = ((ClassValue)parameter).FieldValues;
            else
                fields = ((ValueType)parameter).FieldValues;

            return fields;
        }
    }
}
