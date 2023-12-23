using System;
using System.Threading;

namespace DebugTools.Profiler
{
    class XmlFileProfilerTarget : IProfilerTarget
    {
        public string Name => config.Path;
        public int? ProcessId => null;
        public bool IsAlive => false;

        private FileXmlProfilerReaderConfig config;

        public XmlFileProfilerTarget(FileXmlProfilerReaderConfig config)
        {
            this.config = config;
        }

        public void Start(Action startCallback, CancellationToken cancellationToken)
        {
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
