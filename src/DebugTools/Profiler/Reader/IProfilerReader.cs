using System;
using DebugTools.Tracing;

namespace DebugTools.Profiler
{
    /// <summary>
    /// Provides facilities for reading incoming events from the profiler.
    /// </summary>
    interface IProfilerReader : IDisposable
    {
        IProfilerReaderConfig Config { get; }

        void Initialize();
        void InitializeGlobal();
        void Execute();
        void Stop();

        IProfilerTarget CreateTarget();

        event Action<MethodInfoArgs> MethodInfo;
        event Action<MethodInfoDetailedArgs> MethodInfoDetailed;

        event Action<ModuleLoadedArgs> ModuleLoaded;

        event Action<CallArgs> CallEnter;
        event Action<CallArgs> CallLeave;
        event Action<CallArgs> Tailcall;

        event Action<CallDetailedArgs> CallEnterDetailed;
        event Action<CallDetailedArgs> CallLeaveDetailed;
        event Action<CallDetailedArgs> TailcallDetailed;

        event Action<UnmanagedTransitionArgs> ManagedToUnmanaged;
        event Action<UnmanagedTransitionArgs> UnmanagedToManaged;

        event Action<ExceptionArgs> Exception;
        event Action<CallArgs> ExceptionFrameUnwind;
        event Action<ExceptionCompletedArgs> ExceptionCompleted;

        event Action<StaticFieldValueArgs> StaticFieldValue;

        event Action<ThreadArgs> ThreadCreate;
        event Action<ThreadArgs> ThreadDestroy;
        event Action<ThreadNameArgs> ThreadName;

        event Action<ShutdownArgs> Shutdown;

        event Action Completed;
    }
}
