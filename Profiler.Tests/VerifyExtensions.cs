using DebugTools;
using DebugTools.Profiler;

namespace Profiler.Tests
{
    internal static class VerifyExtensions
    {
        public static FrameVerifier Verify(this IFrame frame)
        {
            return new FrameVerifier(frame);
        }

        public static SigMethodVerifier Verify(this SigMethod method)
        {
            return new SigMethodVerifier(method);
        }
    }
}