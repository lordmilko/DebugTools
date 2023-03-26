using System.Linq;
using DebugTools.Profiler;

namespace Profiler.Tests
{
    class MockProfilerSession : ProfilerSession
    {
        public MockProfilerSession(RootFrame[] frames)
        {
            LastTrace = frames.Select(f => new ThreadStack(false, 1000)
            {
                Current = f
            }).ToArray();

            Process = System.Diagnostics.Process.GetCurrentProcess();
        }
    }
}
