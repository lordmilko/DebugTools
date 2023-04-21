using System.Diagnostics;

namespace DebugTools.Profiler
{
    class LiveProfilerReaderConfig : IProfilerReaderConfig
    {
        public ProfilerSessionType SessionType { get; }

        public Process Process { get; set; }

        public string ProcessName { get; }

        public string FileName { get; set; }

        public int PipeTimeout { get; set; } = 10000;

        public ProfilerSetting[] Settings { get; }

        public LiveProfilerReaderConfig(ProfilerSessionType sessionType, string processName, params ProfilerSetting[] settings)
        {
            SessionType = sessionType;
            ProcessName = processName;
            Settings = settings;
        }
    }
}
