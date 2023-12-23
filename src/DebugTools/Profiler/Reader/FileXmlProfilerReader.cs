using System;
using DebugTools.Tracing;

namespace DebugTools.Profiler
{
    class FileXmlProfilerReader : IProfilerReader
    {
        public IProfilerReaderConfig Config { get; }

        public FileXmlProfilerReader(FileXmlProfilerReaderConfig config)
        {
            Config = config;
        }

        public void Initialize() => throw new NotImplementedException();

        public void InitializeGlobal() => throw new NotImplementedException();

        public void Execute() => throw new NotImplementedException();

        public void Stop() => throw new NotImplementedException();

        public IProfilerTarget CreateTarget() => new XmlFileProfilerTarget((FileXmlProfilerReaderConfig) Config);

        public void Dispose()
        {
        }

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
#pragma warning enable CS0067
    }
}
