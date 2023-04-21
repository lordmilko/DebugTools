using DebugTools.Tracing;
using Microsoft.Diagnostics.Tracing;

namespace DebugTools.Profiler
{
    class FileEtwProfilerReader : EtwProfilerReader
    {
        private ETWTraceEventSource source;

        public new FileEtwProfilerReaderConfig Config => (FileEtwProfilerReaderConfig) base.Config;

        public FileEtwProfilerReader(FileEtwProfilerReaderConfig config) : base(config)
        {
        }

        public override void Initialize()
        {
            source = new ETWTraceEventSource(Config.FileName);

            Parser = new ProfilerTraceEventParser(source);
        }

        public override void Execute()
        {
            source.Process();
        }

        public override void Stop()
        {
            source.Dispose();
        }

        public override IProfilerTarget CreateTarget() => new EtwFileProfilerTarget(Config);
    }
}
