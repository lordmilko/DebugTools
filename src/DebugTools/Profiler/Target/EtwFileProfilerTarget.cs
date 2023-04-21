using System;
using System.Threading;

namespace DebugTools.Profiler
{
    class EtwFileProfilerTarget : IProfilerTarget
    {
        public string Name => config.FileName;
        public int? ProcessId { get; }
        public bool IsAlive => false; //Must be false so that GetImplicitProfilerSession doesn't see us

        private FileEtwProfilerReaderConfig config;

        public EtwFileProfilerTarget(FileEtwProfilerReaderConfig config)
        {
            this.config = config;
        }

        public void Start(Action startCallback, CancellationToken cancellationToken)
        {
            startCallback();
        }

        public void SetExitHandler(Action<bool> onTargetExit)
        {
        }

        public void ExecuteCommand(MessageType messageType, object value)
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
        }
    }
}
