namespace DebugTools.Profiler
{
    public class ProfilerSetting
    {
        public static readonly ProfilerSetting WaitForDebugger = new ProfilerSetting(ProfilerEnvFlags.WaitForDebugger, null);
        public static readonly ProfilerSetting Detailed = new ProfilerSetting(ProfilerEnvFlags.Detailed, null);

        public ProfilerEnvFlags Flag { get; }

        public string Value { get; }

        public ProfilerSetting(ProfilerEnvFlags flag, object value)
        {
            Flag = flag;
            Value = value?.ToString();
        }
    }
}