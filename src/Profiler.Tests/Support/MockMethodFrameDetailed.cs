using System;
using System.Collections.Generic;
using DebugTools.Profiler;

namespace Profiler.Tests
{
    class MockMethodFrameDetailed : IMethodFrameDetailedInternal
    {
        public List<object> Parameters { get; }

        public object ReturnValue { get; }

        public MockMethodFrameDetailed(IMethodInfoDetailed methodInfo, List<object> parameters, object returnValue)
        {
            if (methodInfo == null)
                throw new ArgumentNullException(nameof(methodInfo));

            MethodInfo = methodInfo;
            Parameters = parameters;
            ReturnValue = returnValue;
        }

        #region IFrame

        public IFrame Parent { get; set; }
        public List<IMethodFrame> Children { get; set; } = new List<IMethodFrame>();
        public long Sequence { get; set; }
        public IRootFrame GetRoot()
        {
            var parent = Parent;

            while (true)
            {
                if (parent is IRootFrame)
                    return (IRootFrame)parent;

                parent = parent.Parent;
            }
        }

        #endregion
        #region IMethodFrame

        public IMethodInfo MethodInfo { get; }
        public IMethodFrame CloneWithNewParent(IFrame newParent)
        {
            return new MockMethodFrameDetailed((IMethodInfoDetailed) MethodInfo, Parameters, ReturnValue)
            {
                Parent = newParent,
                Sequence = Sequence
            };
        }

        #endregion
        #region IMethodFrameDetailed

        public List<object> GetEnterParameters() => Parameters;

        public object GetExitResult() => ReturnValue;

        #endregion

        public byte[] EnterValue { get; set; }
        public byte[] ExitValue { get; set; }

        public override string ToString()
        {
            return MethodFrameStringWriter.Default.ToString(this);
        }
    }
}
