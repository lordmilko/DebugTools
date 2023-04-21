using System.Diagnostics;
using System.Linq;
using DebugTools.Profiler;

namespace Profiler.Tests
{
    class MockProfilerSession : ProfilerSession
    {
        public MockProfilerSession(RootFrame[] frames) : base(new LiveProfilerReaderConfig(ProfilerSessionType.Normal, null))
        {
            LastTrace = frames.Select(f => new ThreadStack(false, 1000)
            {
                Current = f
            }).ToArray();

            var config = new LiveProfilerReaderConfig(ProfilerSessionType.Normal, null)
            {
                Process = Process.GetCurrentProcess()
            };

            Target = new LiveProfilerTarget(config);
        }
    }
}
