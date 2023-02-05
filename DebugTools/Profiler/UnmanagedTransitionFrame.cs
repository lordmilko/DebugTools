namespace DebugTools.Profiler
{
    public class UnmanagedTransitionFrame : MethodFrame, IUnmanagedTransitionFrame
    {
        public FrameKind Kind { get; }

        public UnmanagedTransitionFrame(IMethodInfo methodInfo, long sequence, FrameKind kind) : base(methodInfo, sequence)
        {
            Kind = kind;
        }

        protected UnmanagedTransitionFrame(IFrame newParent, UnmanagedTransitionFrame originalFrame) : base(newParent, originalFrame)
        {
            Kind = originalFrame.Kind;
        }

        public override IMethodFrame CloneWithNewParent(IFrame newParent)
        {
            return new UnmanagedTransitionFrame(newParent, this);
        }
    }
}
