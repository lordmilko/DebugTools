namespace DebugTools.Profiler
{
    public class FileProfilerSession : ProfilerSession
    {
        public override int? PID => null;

        public override string Name { get; }

        public FileProfilerSession(string fileName) : base(ProfilerSessionType.File)
        {
            Name = fileName;
            Process = System.Diagnostics.Process.GetCurrentProcess();
        }
    }
}
