using System;
using System.Collections.Generic;
using DebugTools.Profiler;

namespace Profiler.Tests
{
    class MockUnmanagedTransitionFrame : IUnmanagedTransitionFrame
    {
        public FrameKind Kind { get; }

        public MockUnmanagedTransitionFrame(IMethodInfo methodInfo, FrameKind kind)
        {
            if (methodInfo == null)
                throw new ArgumentNullException(nameof(methodInfo));

            MethodInfo = methodInfo;
            Kind = kind;
        }

        #region IFrame

        public IFrame Parent { get; set; }
        public List<IMethodFrame> Children { get; set; } = new List<IMethodFrame>();
        public long Sequence { get; set; }
        public IRootFrame GetRoot()
        {
            throw new System.NotImplementedException();
        }

        #endregion
        #region IMethodFrame

        public IMethodInfo MethodInfo { get; }
        public IMethodFrame CloneWithNewParent(IFrame newParent)
        {
            return new MockUnmanagedTransitionFrame(MethodInfo, Kind)
            {
                Parent = newParent
            };
        }

        #endregion
        public override string ToString()
        {
            return MethodFrameStringWriter.Default.ToString(this);
        }
    }
}
