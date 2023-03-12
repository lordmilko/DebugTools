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

        public static ThreadVerifier Verify(this ThreadStack thread)
        {
            return new ThreadVerifier(thread);
        }

        public static ValueVerifier VerifyValue(this object value)
        {
            return new ValueVerifier(value);
        }

        public static SigMethodVerifier Verify(this SigMethod method)
        {
            return new SigMethodVerifier(method);
        }

        public static BlacklistVerifier Verify(this string[] moduleNames)
        {
            return new BlacklistVerifier(moduleNames);
        }
    }
}
