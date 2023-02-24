namespace DebugTools.Profiler
{
    public class ProfilerSetting
    {
        public static readonly ProfilerSetting WaitForDebugger = new ProfilerSetting(ProfilerEnvFlags.WaitForDebugger, null);
        public static readonly ProfilerSetting Detailed = new ProfilerSetting(ProfilerEnvFlags.Detailed, null);
        public static readonly ProfilerSetting TraceStart = new ProfilerSetting(ProfilerEnvFlags.TraceStart, null);
        public static readonly ProfilerSetting IgnoreDefaultBlacklist = new ProfilerSetting(ProfilerEnvFlags.IgnoreDefaultBlacklist, null);
        public static readonly ProfilerSetting DisablePipe = new ProfilerSetting(ProfilerEnvFlags.DisablePipe, null);
        public static readonly ProfilerSetting IncludeUnknownUnmanagedTransitions = new ProfilerSetting(ProfilerEnvFlags.IncludeUnknownUnmanagedTransitions, null);
        public static readonly ProfilerSetting Minimized = new ProfilerSetting(ProfilerEnvFlags.Minimized, null);

        public ProfilerEnvFlags Flag { get; }

        public object Value { get; }

        public string StringValue => Value?.ToString();

        public ProfilerSetting(ProfilerEnvFlags flag, object value)
        {
            Flag = flag;
            Value = value;
        }

        public static ProfilerSetting ModuleBlacklist(MatchCollection collection)
        {
            return new ProfilerSetting(ProfilerEnvFlags.ModuleBlacklist, collection);
        }

        public static ProfilerSetting ModuleWhitelist(MatchCollection collection)
        {
            return new ProfilerSetting(ProfilerEnvFlags.ModuleWhitelist, collection);
        }
    }
}
