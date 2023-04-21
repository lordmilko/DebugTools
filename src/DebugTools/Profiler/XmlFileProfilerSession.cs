namespace DebugTools.Profiler
{
    public class XmlFileProfilerSession : ProfilerSession
    {
        public override int? PID => null;

        public override string Name { get; }

        public XmlFileProfilerSession(string fileName) : base(new LiveProfilerReaderConfig(ProfilerSessionType.XmlFile, null))
        {
            Name = fileName;
        }
    }
}
