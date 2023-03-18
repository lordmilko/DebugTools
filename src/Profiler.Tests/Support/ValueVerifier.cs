using System;
using System.Collections.Generic;
using System.Linq;
using DebugTools.Profiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Profiler.Tests
{
    struct ValueVerifier
    {
        private object v;

        public ValueVerifier(object value)
        {
            v = value;
        }

        public void HasValue<T>(T value)
        {
            var valueObj = (IValue<T>)v;

            Assert.AreEqual(value, valueObj.Value);
        }

        public ValueVerifier HasClassType(string name)
        {
            var classObj = (ClassValue) v;

            Assert.AreEqual(name, classObj.Name);

            return this;
        }

        public ValueVerifier HasValueType(string name)
        {
            var valueObj = (StructValue) v;

            Assert.AreEqual(name, valueObj.Name);

            return this;
        }

        public void HasFieldValue<T>(T value)
        {
            var fields = GetFields();

            Assert.AreEqual(1, fields.Count, "Expected number of fields was incorrect");

            var valueObj = (IValue<T>)fields[0];

            Assert.AreEqual(value, valueObj.Value);
        }

        public void HasFieldValue(MaxTraceDepth value)
        {
            var fields = GetFields();

            Assert.AreEqual(1, fields.Count, "Expected number of fields was incorrect");

            var field = fields[0];

            Assert.AreEqual(value, field);
        }

        public void HasFieldValue(Action<ValueVerifier> action)
        {
            var fields = GetFields();

            Assert.AreEqual(1, fields.Count, "Expected number of fields was incorrect");

            var valueObj = fields[0];

            action(valueObj.VerifyValue());
        }

        public ValueVerifier HasFieldValue<T>(int index, T value)
        {
            var fields = GetFields();

            if (fields.Count < index)
                Assert.Fail($"Expected fields to have at least {index + 1} element(s). Actual: {fields.Count}");

            var valueObj = (IValue<T>)fields[index];

            Assert.AreEqual(value, valueObj.Value);

            return this;
        }

        public void VerifyArray(params Action<ValueVerifier>[] actions)
        {
            var array = (SZArrayValue) v;

            Assert.AreEqual(actions.Length, array.Value.Length);

            for (var i = 0; i < actions.Length; i++)
            {
                actions[i](array.Value[i].VerifyValue());
            }
        }

        public void VerifyMultiArray(params Action<ValueVerifier>[] actions)
        {
            var array = (ArrayValue)v;

            Assert.AreEqual(actions.Length, array.Value.Length);

            var items = array.Value.Cast<object>().ToArray();

            for (var i = 0; i < items.Length; i++)
            {
                actions[i].Invoke(items[i].VerifyValue());
            }
        }

        public ValueVerifier HasPtrDisplay(string expected)
        {
            Assert.IsInstanceOfType(v, typeof(PtrValue));

            var str = v.ToString();

            Assert.AreEqual(expected, str);

            return this;
        }

        public ValueVerifier HasPtrValue<T>(T value)
        {
            var ptr = (PtrValue)v;

            var valueObj = (IValue<T>) ptr.Value;

            Assert.AreEqual(value, valueObj.Value);

            return this;
        }

        public ValueVerifier HasPtrValue(Action<ValueVerifier> verifyElm)
        {
            var ptr = (PtrValue) v;

            verifyElm(ptr.Value.VerifyValue());

            return this;
        }

        public ValueVerifier HasFieldValue(int index, Action<ValueVerifier> action)
        {
            var fields = GetFields();

            if (fields.Count < index)
                Assert.Fail($"Expected fields to have at least {index + 1} element(s). Actual: {fields.Count}");

            var valueObj = fields[index];

            action(valueObj.VerifyValue());

            return this;
        }

        private List<object> GetFields()
        {
            return ((ComplexTypeValue)v).FieldValues;
        }
    }
}
