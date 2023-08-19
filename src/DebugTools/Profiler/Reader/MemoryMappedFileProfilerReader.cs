using System;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;
using DebugTools.Tracing;
using Microsoft.Diagnostics.Tracing;

namespace DebugTools.Profiler
{
    class MemoryMappedFileProfilerReader : IProfilerReader
    {
#pragma warning disable CS0067
        public event Action<MethodInfoArgs> MethodInfo;
        public event Action<MethodInfoDetailedArgs> MethodInfoDetailed;
        public event Action<ModuleLoadedArgs> ModuleLoaded;
        public event Action<CallArgs> CallEnter;
        public event Action<CallArgs> CallLeave;
        public event Action<CallArgs> Tailcall;
        public event Action<CallDetailedArgs> CallEnterDetailed;
        public event Action<CallDetailedArgs> CallLeaveDetailed;
        public event Action<CallDetailedArgs> TailcallDetailed;
        public event Action<UnmanagedTransitionArgs> ManagedToUnmanaged;
        public event Action<UnmanagedTransitionArgs> UnmanagedToManaged;
        public event Action<ExceptionArgs> Exception;
        public event Action<CallArgs> ExceptionFrameUnwind;
        public event Action<ExceptionCompletedArgs> ExceptionCompleted;
        public event Action<StaticFieldValueArgs> StaticFieldValue;
        public event Action<ThreadArgs> ThreadCreate;
        public event Action<ThreadArgs> ThreadDestroy;
        public event Action<ThreadNameArgs> ThreadName;
        public event Action<ShutdownArgs> Shutdown;
        public event Action Completed;
#pragma warning restore CS0067

        public IProfilerReaderConfig Config { get; }

        public LiveProfilerReaderConfig LiveConfig => (LiveProfilerReaderConfig) Config;

        private CancellationTokenSource cts = new CancellationTokenSource();

        private MemoryMappedFile mmf;
        private MemoryMappedViewAccessor mma;
        private EventWaitHandle hasDataEvent;
        private EventWaitHandle wasProcessedEvent;

        public MemoryMappedFileProfilerReader(LiveProfilerReaderConfig config)
        {
            Config = config;
        }

        public void Initialize()
        {
            var ppid = Process.GetCurrentProcess().Id;
            var pid = LiveConfig.Process.Id;

            mmf = MemoryMappedFile.CreateNew($"DebugToolsMemoryMappedFile_Profiler_{ppid}_{pid}", 1000000000);
            mma = mmf.CreateViewAccessor();

            hasDataEvent = new EventWaitHandle(false, EventResetMode.AutoReset, $"DebugToolsProfilerHasDataEvent_Profiler_{ppid}_{pid}");
            wasProcessedEvent = new EventWaitHandle(false, EventResetMode.AutoReset, $"DebugToolsProfilerWasProcessedEvent_Profiler_{ppid}_{pid}");
        }

        public void InitializeGlobal()
        {
            throw new NotSupportedException($"Global profiling is not supported with a profiler reader of type '{GetType().Name}'.");
        }

        public unsafe void Execute()
        {
            while (!cts.IsCancellationRequested)
            {
                WaitHandle.WaitAny(new[]{cts.Token.WaitHandle, hasDataEvent});

                if (cts.IsCancellationRequested)
                    break;

                var pos = 0;

                var numEntries = mma.ReadUInt32(pos);
                pos += 4;

                for (var i = 0; i < numEntries; i++)
                {
                    var entrySize = mma.ReadUInt32(pos);
                    pos += 4;

                    //Both the C++ and C# header have trailing padding
                    MMFEventHeader header;
                    mma.Read(pos, out header);
                    pos += Marshal.SizeOf<MMFEventHeader>();

                    byte* blobPtr = default;
                    mma.SafeMemoryMappedViewHandle.AcquirePointer(ref blobPtr);
                    blobPtr += pos;

                    try
                    {
                        var data = FakeTraceEventProvider.GetEvent(
                            ref header,
                            blobPtr
                        );

                        DispatchEvent(header.EventType, data);
                    }
                    finally
                    {
                        mma.SafeMemoryMappedViewHandle.ReleasePointer();
                    }

                    pos += header.UserDataSize;
                }

                wasProcessedEvent.Set();
            }
        }

        private void DispatchEvent(int eventType, TraceEvent data)
        {
            switch (eventType)
            {
                case ProfilerTraceEventParser.EventId.CallEnter:
                    CallEnter?.Invoke((CallArgs)data);
                    break;

                case ProfilerTraceEventParser.EventId.CallExit:
                    CallLeave?.Invoke((CallArgs)data);
                    break;

                case ProfilerTraceEventParser.EventId.Tailcall:
                    Tailcall?.Invoke((CallArgs)data);
                    break;

                case ProfilerTraceEventParser.EventId.CallEnterDetailed:
                    CallEnterDetailed?.Invoke((CallDetailedArgs)data);
                    break;

                case ProfilerTraceEventParser.EventId.CallExitDetailed:
                    CallLeaveDetailed?.Invoke((CallDetailedArgs)data);
                    break;

                case ProfilerTraceEventParser.EventId.TailcallDetailed:
                    TailcallDetailed?.Invoke((CallDetailedArgs)data);
                    break;

                case ProfilerTraceEventParser.EventId.ManagedToUnmanaged:
                    ManagedToUnmanaged?.Invoke((UnmanagedTransitionArgs)data);
                    break;

                case ProfilerTraceEventParser.EventId.UnmanagedToManaged:
                    UnmanagedToManaged?.Invoke((UnmanagedTransitionArgs)data);
                    break;

                case ProfilerTraceEventParser.EventId.Exception:
                    Exception?.Invoke((ExceptionArgs)data);
                    break;

                case ProfilerTraceEventParser.EventId.ExceptionFrameUnwind:
                    ExceptionFrameUnwind?.Invoke((CallArgs)data);
                    break;

                case ProfilerTraceEventParser.EventId.ExceptionCompleted:
                    ExceptionCompleted?.Invoke((ExceptionCompletedArgs)data);
                    break;

                case ProfilerTraceEventParser.EventId.StaticFieldValue:
                    StaticFieldValue?.Invoke((StaticFieldValueArgs)data);
                    break;

                case ProfilerTraceEventParser.EventId.MethodInfo:
                    MethodInfo?.Invoke((MethodInfoArgs)data);
                    break;

                case ProfilerTraceEventParser.EventId.MethodInfoDetailed:
                    MethodInfoDetailed?.Invoke((MethodInfoDetailedArgs)data);
                    break;

                case ProfilerTraceEventParser.EventId.ModuleLoaded:
                    ModuleLoaded?.Invoke((ModuleLoadedArgs)data);
                    break;

                case ProfilerTraceEventParser.EventId.ThreadCreate:
                    ThreadCreate?.Invoke((ThreadArgs)data);
                    break;

                case ProfilerTraceEventParser.EventId.ThreadDestroy:
                    ThreadDestroy?.Invoke((ThreadArgs)data);
                    break;

                case ProfilerTraceEventParser.EventId.ThreadName:
                    ThreadName?.Invoke((ThreadNameArgs)data);
                    break;

                case ProfilerTraceEventParser.EventId.Shutdown:
                    Shutdown?.Invoke((ShutdownArgs)data);
                    break;

                default:
                    throw new NotImplementedException($"Don't know how to handle event '{eventType}'.");
            }
        }

        public void Stop()
        {
            cts.Cancel();
        }

        public IProfilerTarget CreateTarget() => new LiveProfilerTarget(LiveConfig);

        public void Dispose()
        {
            cts.Cancel();
            mma?.Dispose();
            mmf?.Dispose();
            hasDataEvent?.Dispose();
            wasProcessedEvent?.Dispose();
        }
    }
}
