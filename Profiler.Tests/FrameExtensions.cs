using DebugTools.Profiler;

namespace Profiler.Tests
{
    internal static class FrameExtensions
    {
        public static FrameVerifier Verify(this IFrame frame)
        {
            return new FrameVerifier(frame);
        }
    }
}