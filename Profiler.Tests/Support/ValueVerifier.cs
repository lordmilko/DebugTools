using System;
using System.Collections.Generic;
using DebugTools.Profiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ValueType = DebugTools.Profiler.ValueType;

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

        public void HasFieldValue<T>(T value)
        {
            var fields = GetFields();

            Assert.AreEqual(1, fields.Count, "Expected number of fields was incorrect");

            var valueObj = (IValue<T>)fields[0];

            Assert.AreEqual(value, valueObj.Value);
        }

        public void HasFieldValue(Action<ValueVerifier> action)
        {
            var fields = GetFields();

            Assert.AreEqual(1, fields.Count, "Expected number of fields was incorrect");

            var valueObj = fields[0];

            action(valueObj.VerifyValue());
        }

        public void HasFieldValue<T>(int index, T value)
        {
            var fields = GetFields();

            if (fields.Count < index)
                Assert.Fail($"Expected fields to have at least {index + 1} element(s). Actual: {fields.Count}");

            var valueObj = (IValue<T>)fields[index];

            Assert.AreEqual(value, valueObj.Value);
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

        private List<object> GetFields()
        {
            List<object> fields;

            if (v is ClassValue)
                fields = ((ClassValue)v).FieldValues;
            else
                fields = ((ValueType)v).FieldValues;

            return fields;
        }
    }
}
