using System;
using System.Diagnostics;
using DebugTools.Tracing;

namespace DebugTools.Profiler
{
    abstract class EtwProfilerReader : IProfilerReader
    {
        public IProfilerReaderConfig Config { get; }

        protected ProfilerTraceEventParser Parser { get; set; }

        #region Events
        #region MethodInfo

        public event Action<MethodInfoArgs> MethodInfo
        {
            add => Parser.MethodInfo += value;
            remove => Parser.MethodInfo -= value;
        }

        public event Action<MethodInfoDetailedArgs> MethodInfoDetailed
        {
            add => Parser.MethodInfoDetailed += value;
            remove => Parser.MethodInfoDetailed -= value;
        }

        #endregion

        public event Action<ModuleLoadedArgs> ModuleLoaded
        {
            add => Parser.ModuleLoaded += value;
            remove => Parser.ModuleLoaded -= value;
        }

        #region Call

        public event Action<CallArgs> CallEnter
        {
            add => Parser.CallEnter += value;
            remove => Parser.CallEnter -= value;
        }

        public event Action<CallArgs> CallLeave
        {
            add => Parser.CallLeave += value;
            remove => Parser.CallLeave -= value;
        }

        public event Action<CallArgs> Tailcall
        {
            add => Parser.Tailcall += value;
            remove => Parser.Tailcall -= value;
        }

        #endregion
        #region CallDetailed

        public event Action<CallDetailedArgs> CallEnterDetailed
        {
            add => Parser.CallEnterDetailed += value;
            remove => Parser.CallEnterDetailed -= value;
        }

        public event Action<CallDetailedArgs> CallLeaveDetailed
        {
            add => Parser.CallLeaveDetailed += value;
            remove => Parser.CallLeaveDetailed -= value;
        }

        public event Action<CallDetailedArgs> TailcallDetailed
        {
            add => Parser.TailcallDetailed += value;
            remove => Parser.TailcallDetailed -= value;
        }

        #endregion
        #region Unmanaged

        public event Action<UnmanagedTransitionArgs> ManagedToUnmanaged
        {
            add => Parser.ManagedToUnmanaged += value;
            remove => Parser.ManagedToUnmanaged -= value;
        }

        public event Action<UnmanagedTransitionArgs> UnmanagedToManaged
        {
            add => Parser.UnmanagedToManaged += value;
            remove => Parser.UnmanagedToManaged -= value;
        }

        #endregion
        #region Exception

        public event Action<ExceptionArgs> Exception
        {
            add => Parser.Exception += value;
            remove => Parser.Exception -= value;
        }

        public event Action<CallArgs> ExceptionFrameUnwind
        {
            add => Parser.ExceptionFrameUnwind += value;
            remove => Parser.ExceptionFrameUnwind -= value;
        }

        public event Action<ExceptionCompletedArgs> ExceptionCompleted
        {
            add => Parser.ExceptionCompleted += value;
            remove => Parser.ExceptionCompleted -= value;
        }

        #endregion

        public event Action<StaticFieldValueArgs> StaticFieldValue
        {
            add => Parser.StaticFieldValue += value;
            remove => Parser.StaticFieldValue -= value;
        }

        #region Thread

        public event Action<ThreadArgs> ThreadCreate
        {
            add => Parser.ThreadCreate += value;
            remove => Parser.ThreadCreate -= value;
        }

        public event Action<ThreadArgs> ThreadDestroy
        {
            add => Parser.ThreadDestroy += value;
            remove => Parser.ThreadDestroy -= value;
        }

        public event Action<ThreadNameArgs> ThreadName
        {
            add => Parser.ThreadName += value;
            remove => Parser.ThreadName -= value;
        }

        #endregion

        public event Action<ShutdownArgs> Shutdown
        {
            add => Parser.Shutdown += value;
            remove => Parser.Shutdown -= value;
        }

#pragma warning disable CS0067
        public virtual event Action Completed;
#pragma warning restore CS0067

        #endregion

        protected EtwProfilerReader(IProfilerReaderConfig config)
        {
            Config = config;
        }

        public abstract void Initialize();

        public virtual void InitializeGlobal()
        {
            throw new NotSupportedException($"Global profiling is not supported with a profiler reader of type '{GetType().Name}'.");
        }

        public abstract void Execute();

        public abstract void Stop();

        public abstract IProfilerTarget CreateTarget();

        public virtual void Dispose()
        {
        }
    }
}
