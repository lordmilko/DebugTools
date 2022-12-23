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
            var detailed = (MethodFrameDetailed)frame;

            var serializer = new ValueSerializer(detailed.Value);

            var parameters = serializer.Parameters;

            Assert.AreEqual(1, parameters.Count);

            var valueObj = (IValue<T>) parameters[0];

            Assert.AreEqual(value, valueObj.Value);
        }

        public void HasArrayValues<T>(CorElementType type, params T[] values)
        {
            var detailed = (MethodFrameDetailed)frame;

            var serializer = new ValueSerializer(detailed.Value);

            var parameters = serializer.Parameters;

            Assert.AreEqual(1, parameters.Count);

            var arrObj = (SZArrayValue)parameters[0];

            Assert.AreEqual(values.Length, arrObj.Value.Length, "Expected number of array elements was incorrect");

            for (var i = 0; i < values.Length; i++)
            {
                var valueObj = (IValue<T>) arrObj.Value[i];

                Assert.AreEqual(values[i], valueObj.Value);
            }
        }

        public void HasArrayClassValues(CorElementType type, params string[] classNames)
        {
            var detailed = (MethodFrameDetailed)frame;

            var serializer = new ValueSerializer(detailed.Value);

            var parameters = serializer.Parameters;

            Assert.AreEqual(1, parameters.Count);

            var arrObj = (SZArrayValue)parameters[0];

            Assert.AreEqual(classNames.Length, arrObj.Value.Length, "Expected number of array elements was incorrect");

            for (var i = 0; i < classNames.Length; i++)
            {
                var valueObj = (ClassValue)arrObj.Value[i];

                Assert.AreEqual(classNames[i], valueObj.Name);
            }
        }
    }
}
