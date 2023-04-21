namespace DebugTools.Profiler
{
    class FileEtwProfilerReaderConfig : IProfilerReaderConfig
    {
        public ProfilerSessionType SessionType { get; }

        public string FileName { get; }

        public ProfilerSetting[] Settings { get; }
    }
}
