using System;
using System.Collections.Generic;
using DebugTools.Profiler;

namespace Profiler.Tests
{
    class MockMethodFrameDetailed : IMethodFrameDetailed
    {
        private static long nextSequence;

        public List<object> Parameters { get; }

        public object ReturnValue { get; }

        public MockMethodFrameDetailed(IMethodInfoDetailed methodInfo, List<object> parameters, object returnValue)
        {
            if (methodInfo == null)
                throw new ArgumentNullException(nameof(methodInfo));

            MethodInfo = methodInfo;
            Parameters = parameters;
            ReturnValue = returnValue;

            Sequence = ++nextSequence;
        }

        #region IFrame

        public IFrame Parent { get; set; }
        public List<IMethodFrame> Children { get; set; } = new List<IMethodFrame>();
        public long Sequence { get; }
        public IRootFrame GetRoot()
        {
            throw new System.NotImplementedException();
        }

        #endregion
        #region IMethodFrame

        public IMethodInfo MethodInfo { get; }
        public IMethodFrame CloneWithNewParent(IFrame newParent)
        {
            return new MockMethodFrameDetailed((IMethodInfoDetailed) MethodInfo, Parameters, ReturnValue)
            {
                Parent = newParent
            };
        }

        #endregion
        #region IMethodFrameDetailed

        public List<object> GetEnterParameters() => Parameters;

        public object GetExitResult() => ReturnValue;

        #endregion

        public override string ToString()
        {
            return MethodFrameStringWriter.Default.ToString(this);
        }
    }
}
