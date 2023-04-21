using System;
using System.Threading;

namespace DebugTools.Profiler
{
    public interface IProfilerTarget : IDisposable
    {
        string Name { get; }

        int? ProcessId { get; }

        bool IsAlive { get; }

        void Start(Action startCallback, CancellationToken cancellationToken);

        void SetExitHandler(Action<bool> onTargetExit);

        void ExecuteCommand(MessageType messageType, object value);
    }
}
