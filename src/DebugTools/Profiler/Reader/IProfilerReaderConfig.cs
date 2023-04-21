namespace DebugTools.Profiler
{
    public interface IProfilerReaderConfig
    {
        ProfilerSessionType SessionType { get; }

        ProfilerSetting[] Settings { get; }
    }
}
