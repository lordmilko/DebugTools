using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using ClrDebug;
using DebugTools.Tracing;
using Microsoft.Diagnostics.Tracing.Session;

namespace DebugTools.Profiler
{
    public class ProfilerSession : IDisposable
    {
        private static int maxId;
        private const string SessionPrefix = "DebugTools_Profiler_";

        public Process Process { get; set; }

        public TraceEventSession TraceEventSession { get; }

        public Thread Thread { get; }

        internal ConcurrentDictionary<long, IMethodInfoInternal> Methods { get; } = new ConcurrentDictionary<long, IMethodInfoInternal>();

        //Only populated in detailed mode
        internal ConcurrentDictionary<int, ModuleInfo> Modules { get; } = new ConcurrentDictionary<int, ModuleInfo>();

        public bool HasExited => Process?.HasExited ?? true;

        private NamedPipeClientStream pipe;

        public Dictionary<int, ThreadStack> ThreadCache { get; } = new Dictionary<int, ThreadStack>();
        private Dictionary<int, string> threadNames = new Dictionary<int, string>();

        private bool collectStackTrace;
        private bool includeUnknownTransitions;
        private bool stopping;
        private bool cancelIfTimeoutNoEvents;
        private DateTime stopTime;

        private CancellationTokenSource traceCTS;
        private CancellationTokenSource userCTS;

        private DateTime lastEvent;
        private Thread cancelThread;

        private Exception threadProcException;
        private BlockingCollection<IFrame> watchQueue;
        private bool global;
        private bool disposing;

        public ThreadStack[] LastTrace { get; private set; }

        public ProfilerSession()
        {
            var sessionName = GetNextSessionName();

            TryCloseSession(sessionName);

            //Events MUST be received in the order they are dispatched, otherwise our shadow stack will completely be broken. It's unclear whether
            //the number of ELT events are generate exceeds the threshold specified by NoPerProcessorBuffering (it probably does) but the reliability
            //of receiving ETW events in the correct order is most important. If events get dropped because of this setting, we will end up throwing
            TraceEventSession = new TraceEventSession(sessionName, TraceEventSessionOptions.Create | TraceEventSessionOptions.NoPerProcessorBuffering);

            var parser = new ProfilerTraceEventParser(TraceEventSession.Source);

            parser.MethodInfo += Parser_MethodInfo;
            parser.MethodInfoDetailed += Parser_MethodInfoDetailed;

            parser.ModuleLoaded += Parser_ModuleLoaded;

            parser.CallEnter += Parser_CallEnter;
            parser.CallLeave += Parser_CallLeave;
            parser.Tailcall += Parser_Tailcall;

            parser.CallEnterDetailed += Parser_CallEnterDetailed;
            parser.CallLeaveDetailed += Parser_CallLeaveDetailed;
            parser.TailcallDetailed += Parser_TailcallDetailed;

            parser.ManagedToUnmanaged += args => Parser_UnmanagedTransition(args, FrameKind.M2U);
            parser.UnmanagedToManaged += args => Parser_UnmanagedTransition(args, FrameKind.U2M);

            parser.Exception += Parser_Exception;
            parser.ExceptionFrameUnwind += Parser_ExceptionFrameUnwind;
            parser.ExceptionCompleted += Parser_ExceptionCompleted;

            parser.ThreadName += v =>
            {
                threadNames[v.ThreadID] = v.ThreadName;

                if (ThreadCache.TryGetValue(v.ThreadID, out var stack))
                    stack.Root.ThreadName = v.ThreadName;
            };

            parser.Shutdown += v =>
            {
                //If we're monitoring sessions globally, we don't care if a given process exits, we want to keep
                //watching for the next process
                if (!global)
                {
                    //Calling Dispose() guarantees an immediate stop
                    TraceEventSession.Dispose();

                    traceCTS?.Cancel();
                    userCTS?.Cancel();
                }
            };

            Thread = new Thread(ThreadProc) {Name = "ETWThreadProc"};
            cancelThread = new Thread(CancelTraceThreadProc) {Name = "CancelThreadProc"};
        }

        #region MethodInfo

        private void Parser_MethodInfo(MethodInfoArgs v)
        {
            var wasUnknown = Methods.TryGetValue(v.FunctionID, out var existing) && existing is UnknownMethodInfo;

            //If the function was unknown previously (which can occur in weird double unmanaged to managed transitions, such as with
            //Visual Studio's VsAppDomainManager.OnStart()) we'll overwrite it, and be sure to check against the previous instance by looking at the function id,
            //not the object reference
            Methods[v.FunctionID] = new MethodInfo(new FunctionID(new IntPtr(v.FunctionID)), v.ModuleName, v.TypeName, v.MethodName)
            {
                WasUnknown = wasUnknown
            };
        }

        private void Parser_MethodInfoDetailed(MethodInfoDetailedArgs v)
        {
            var wasUnknown = Methods.TryGetValue(v.FunctionID, out var existing) && existing is UnknownMethodInfo;

            //If the function was unknown previously (which can occur in weird double unmanaged to managed transitions, such as with
            //Visual Studio's VsAppDomainManager.OnStart()) we'll overwrite it, and be sure to check against the previous instance by looking at the function id,
            //not the object reference
            Methods[v.FunctionID] = new MethodInfoDetailed(new FunctionID(new IntPtr(v.FunctionID)), v.ModuleName, v.TypeName, v.MethodName, v.Token, v.SigBlob, v.SigBlobLength)
            {
                WasUnknown = wasUnknown
            };
        }

        #endregion

        private void Parser_ModuleLoaded(ModuleLoadedArgs args)
        {
            Modules[args.UniqueModuleID] = new ModuleInfo(args.UniqueModuleID, args.Path);
        }

        #region CallArgs

        private void Parser_CallEnter(CallArgs args) =>
            CallEnterCommon(args, (t, v, m) => t.Enter(v, m));

        private void Parser_CallLeave(CallArgs args) =>
            CallLeaveCommon(args, (t, v, m) => t.Leave(v, m));

        private void Parser_Tailcall(CallArgs args) =>
            CallLeaveCommon(args, (t, v, m) => t.Tailcall(v, m));

        private void CallEnterCommon<T>(T args, Func<ThreadStack, T, IMethodInfoInternal, IFrame> addMethod, bool ignoreUnknown = false) where T : ICallArgs
        {
            ProcessStopping(args.TimeStamp);

            if (collectStackTrace)
            {
                Validate(args);

                bool setName = false;

                var method = GetMethodSafe(args.FunctionID);

                if (!ThreadCache.TryGetValue(args.ThreadID, out var threadStack))
                {
                    if (ignoreUnknown && method.WasUnknown && !includeUnknownTransitions)
                        return;

                    threadStack = new ThreadStack(includeUnknownTransitions, args.ThreadID);
                    ThreadCache[args.ThreadID] = threadStack;

                    setName = true;
                }

                var frame = addMethod(threadStack, args, method);

                if (frame != null)
                {
                    var local = watchQueue;
                    local?.Add(frame);
                }

                if (setName && threadNames.TryGetValue(args.ThreadID, out var name))
                    threadStack.Root.ThreadName = name;
            }
        }

        private void CallLeaveCommon<T>(T args, Action<ThreadStack, T, IMethodInfoInternal> leaveMethod, bool validateHR = true) where T : ICallArgs
        {
            ProcessStopping(args.TimeStamp);

            if (collectStackTrace)
            {
                if (validateHR)
                    Validate(args);

                if (ThreadCache.TryGetValue(args.ThreadID, out var threadStack))
                {
                    var method = GetMethodSafe(args.FunctionID);

                    leaveMethod(threadStack, args, method);
                }
            }
        }

        #endregion
        #region CallDetailedArgs

        private void Parser_CallEnterDetailed(CallDetailedArgs args) =>
            CallEnterCommon(args, (t, v, m) => t.EnterDetailed(v, m));

        private void Parser_CallLeaveDetailed(CallDetailedArgs args) =>
            CallLeaveCommon(args, (t, v, m) => t.LeaveDetailed(v, m));

        private void Parser_TailcallDetailed(CallDetailedArgs args) =>
            CallLeaveCommon(args, (t, v, m) => t.TailcallDetailed(v, m));

        private void Validate(ICallArgs args)
        {
            if ((uint) args.HRESULT == (uint) PROFILER_HRESULT.PROFILER_E_UNKNOWN_FRAME)
                throw new ProfilerException("Profiler encountered an unexpected function while processing a Leave/Tailcall. Current stack frame is unknown, profiler cannot continue.", (PROFILER_HRESULT) args.HRESULT);

            if ((uint) args.HRESULT >= 0x80041001 && (uint) args.HRESULT <= 0x80042000)
            {
                switch((PROFILER_HRESULT) args.HRESULT)
                {
                    case PROFILER_HRESULT.PROFILER_E_BUFFERFULL:
                    case PROFILER_HRESULT.PROFILER_E_GENERICCLASSID:
                    case PROFILER_HRESULT.PROFILER_E_UNKNOWN_GENERIC_ARRAY:
                    case PROFILER_HRESULT.PROFILER_E_NO_CLASSID:
                        break;

                    default:
                        throw new ProfilerException((PROFILER_HRESULT)(uint)args.HRESULT);
                }
            }

            switch (args.HRESULT)
            {
                case HRESULT.CORPROF_E_CLASSID_IS_COMPOSITE:
                case HRESULT.COR_E_TYPELOAD:
                    return;
            }

            args.HRESULT.ThrowOnNotOK();
        }

        #endregion
        #region Unmanaged

        private void Parser_UnmanagedTransition(UnmanagedTransitionArgs args, FrameKind kind)
        {
            if (args.Reason == COR_PRF_TRANSITION_REASON.COR_PRF_TRANSITION_CALL)
                CallEnterCommon(args, (t, v, m) => t.EnterUnmanagedTransition(v, m, kind), true);
            else
                CallLeaveCommon(args, (t, v, m) => t.LeaveUnmanagedTransition(v, m));
        }

        #endregion
        #region Exception

        private void Parser_Exception(ExceptionArgs args)
        {
            if (collectStackTrace)
            {
                if (ThreadCache.TryGetValue(args.ThreadID, out var threadStack))
                    threadStack.Exception(args);
            }
        }

        private void Parser_ExceptionFrameUnwind(CallArgs args) =>
            CallLeaveCommon(args, (t, v, m) => t.ExceptionFrameUnwind(v, m), false);

        private void Parser_ExceptionCompleted(ExceptionCompletedArgs v)
        {
            if (collectStackTrace)
            {
                if (ThreadCache.TryGetValue(v.ThreadID, out var threadStack))
                    threadStack.ExceptionCompleted(v);
            }
        }

        #endregion

        private void ProcessStopping(DateTime timeStamp)
        {
            if (stopping)
            {
                lastEvent = DateTime.Now;

                if (timeStamp > stopTime)
                {
                    collectStackTrace = false;
                    stopping = false;
                    traceCTS.Cancel();
                }
            }
        }

        private IMethodInfoInternal GetMethodSafe(long functionId)
        {
            if (Methods.TryGetValue(functionId, out var value))
                return value;

            value = new UnknownMethodInfo(new FunctionID(new IntPtr(functionId)))
            {
                WasUnknown = true
            };
            Methods[functionId] = value;
            return value;
        }

        public void Start(CancellationToken cancellationToken, string processName, ProfilerSetting[] settings)
        {
            collectStackTrace = settings?.Any(s => s == ProfilerSetting.TraceStart) == true;
            includeUnknownTransitions = settings?.Any(s => s == ProfilerSetting.IncludeUnknownUnmanagedTransitions) == true;

            var pipeCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            traceCTS = new CancellationTokenSource();

            Process = ProfilerInfo.CreateProcess(processName, p =>
            {
                //If we're only targeting a specific child process, we can't filter based on the process we just created
                 TraceEventProviderOptions options = IsTargetingChildProcess(settings, processName)
                     ? null
                     : new TraceEventProviderOptions {ProcessIDFilter = new[] {p.Id}};

                TraceEventSession.EnableProvider(ProfilerTraceEventParser.ProviderGuid, options: options);

                Thread.Start();
                cancelThread.Start();

                if (traceCTS != null)
                {
                    cancelIfTimeoutNoEvents = false;
                }

                p.EnableRaisingEvents = true;
                p.Exited += (s, e) =>
                {
                    pipeCTS.Cancel();
                    userCTS?.Cancel();
                    traceCTS?.Token.Register(() =>
                    {
                        //Calling Dispose() guarantees an immediate stop
                        TraceEventSession.Dispose();
                    });

                    //There's no guarantee that any more events are going to be received. If we go 2 seconds without receiving any more events, we'll shut everything down

                    if (traceCTS != null)
                    {
                        lastEvent = DateTime.Now;
                        cancelIfTimeoutNoEvents = true;
                    }
                    else
                    {
                        //If the application crashes early during startup, it might not have transmitted any events to us yet;
                        //give it a few seconds to process some events so we can catch an exception
                        if (p.ExitCode != 0)
                        {
                            Thread.Sleep(3000);
                            TraceEventSession.Dispose();
                        }
                    }
                };
            }, settings);

            //This will wait for the pipe to be created if it doesn't exist yet
            try
            {
                if (!settings?.Any(s => s == ProfilerSetting.DisablePipe) == true)
                {
                    pipe = new NamedPipeClientStream(".", $"DebugToolsProfilerPipe_{Process.Id}", PipeDirection.Out);

                    pipe.ConnectAsync(10000, pipeCTS.Token).GetAwaiter().GetResult();
                }
            }
            catch (OperationCanceledException)
            {
                if (!Process.HasExited)
                    throw;
            }
        }

        private bool IsTargetingChildProcess(ProfilerSetting[] settings, string processName)
        {
            if (settings == null)
                return false;

            var setting = settings.FirstOrDefault(s => s.Flag == ProfilerEnvFlags.TargetProcess);

            if (setting == null)
                return false;

            //processName could be a process and some command line args, however in the simple case it's just a process name.
            //Specifying this property is useful when you want to profile Visual Studio but not any of the processes it spawns
            if (processName.EndsWith(setting.StringValue, StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        public void StartGlobal()
        {
            global = true;
            traceCTS = new CancellationTokenSource();

            TraceEventSession.EnableProvider(ProfilerTraceEventParser.ProviderGuid);

            Thread.Start();
            cancelThread.Start();
        }

        private void CancelTraceThreadProc()
        {
            lastEvent = DateTime.Now;

            while (!disposing)
            {
                if (!cancelIfTimeoutNoEvents)
                {
                    Thread.Sleep(100);
                    continue;
                }

                //If we go more than 2 seconds without receiving a new event after the process has shutdown, we'll assume no more events are incoming
                var threshold = DateTime.Now.AddSeconds(-2);

                if (threshold > lastEvent)
                {
                    traceCTS?.Cancel();
                    cancelIfTimeoutNoEvents = false;
                }
                else
                    Thread.Sleep(100);
            }
        }

        private void ThreadProc()
        {
            try
            {
                threadProcException = null;
                TraceEventSession.Source.Process();

                Debug.WriteLine("########## ETW THREADPROC ENDED ##########");
            }
            catch(Exception ex)
            {
                threadProcException = ex;
                TraceEventSession.Dispose();
                userCTS?.Cancel();
                traceCTS?.Cancel();
            }
        }

        private void TryCloseSession(string sessionName)
        {
            var sessions = TraceEventSession.GetActiveSessionNames().Where(n => n.StartsWith(SessionPrefix)).ToArray();

            var processes = Process.GetProcesses();

            foreach (var session in sessions)
            {
                var match = Regex.Match(session, $"{SessionPrefix}(.+?)_.+?");

                if (match.Success && int.TryParse(match.Groups[1].Value, out var pid))
                {
                    if (processes.All(p => p.Id != pid))
                        TraceEventSession.GetActiveSession(session)?.Stop();
                }
            }
        }

        private string GetNextSessionName()
        {
            return $"{SessionPrefix}{Process.GetCurrentProcess().Id}_" + ++maxId;
        }

        public ThreadStack[] Trace(CancellationTokenSource userCTS)
        {
            this.userCTS = userCTS;

            try
            {
                WithTracing(() =>
                {
                    userCTS.Token.Register(() =>
                    {
                        stopTime = DateTime.Now;
                        stopping = true;
                        lastEvent = DateTime.Now;
                        cancelIfTimeoutNoEvents = true;
                        Console.WriteLine("Stopping...");
                    });

                    if (traceCTS.IsCancellationRequested)
                        userCTS.Cancel();

                    userCTS.Token.WaitHandle.WaitOne();

                    traceCTS.Token.WaitHandle.WaitOne();
                });

                ThrowOnError();

                LastTrace = ThreadCache.Values.ToArray();
            }
            finally
            {
                ThreadCache.Clear();
            }

            return LastTrace;
        }

        public IEnumerable<IFrame> Watch(CancellationTokenSource userCTS, Func<IFrame, bool> predicate)
        {
            this.userCTS = userCTS;

            userCTS.Token.Register(() =>
            {
                stopTime = DateTime.Now;
                stopping = true;
                lastEvent = DateTime.Now;
                cancelIfTimeoutNoEvents = true;
                Console.WriteLine("Stopping...");
            });

            watchQueue = new BlockingCollection<IFrame>();

            return WithTracing(() => WatchInternal(predicate));
        }

        private IEnumerable<IFrame> WatchInternal(Func<IFrame, bool> predicate)
        {
            try
            {
                while (true)
                {
                    userCTS.Token.ThrowIfCancellationRequested();

                    var frame = watchQueue.Take(userCTS.Token);

                    if (predicate(frame))
                        yield return frame;
                }
            }
            finally
            {
                watchQueue = null;

                LastTrace = ThreadCache.Values.ToArray();
                ThreadCache.Clear();

                ThrowOnError();
            }
        }

        private void WithTracing(Action action)
        {
            if (Process != null && Process.HasExited)
                return;

            if (traceCTS.IsCancellationRequested)
                traceCTS = new CancellationTokenSource();

            ThreadCache.Clear();

            ExecuteCommand(MessageType.EnableTracing, true);

            collectStackTrace = true;

            try
            {
                action();
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (Process != null && !Process.HasExited)
                    ExecuteCommand(MessageType.EnableTracing, false);
            }
        }

        private IEnumerable<IFrame> WithTracing(Func<IEnumerable<IFrame>> action)
        {
            if (Process?.HasExited == true)
                yield break;

            ExecuteCommand(MessageType.EnableTracing, true);

            collectStackTrace = true;

            try
            {
                foreach (var item in action())
                    yield return item;
            }
            finally
            {
                if (Process != null && !Process.HasExited)
                    ExecuteCommand(MessageType.EnableTracing, false);
            }
        }

        public void Reset()
        {
            traceCTS.Token.WaitHandle.WaitOne();

            cancelIfTimeoutNoEvents = false;
            stopping = false;

            ThreadCache.Clear();

            traceCTS = new CancellationTokenSource();
        }

        public void ThrowOnError()
        {
            if (threadProcException != null)
                throw threadProcException;
        }

        public void ExecuteCommand(MessageType messageType, object value)
        {
            if (pipe == null)
            {
                var original = Console.ForegroundColor;

                try
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;

                    Console.WriteLine("WARNING: Communication Pipe not active. Only startup profiling will be captured");
                }
                finally
                {
                    Console.ForegroundColor = original;
                }

                return;
            }

            var buffer = new byte[1004];

            var bytes = BitConverter.GetBytes((int) messageType);

            var pos = 0;

            for (; pos < bytes.Length; pos++)
                buffer[pos] = bytes[pos];

            switch(value)
            {
                case long l:
                    bytes = BitConverter.GetBytes(l);
                    break;

                case bool b:
                    bytes = BitConverter.GetBytes(b);
                    break;

                default:
                    throw new NotImplementedException($"Don't know how to handle value of type '{value.GetType().Name}'.");
            }

            for (var i = 0; i < bytes.Length && pos < buffer.Length; pos++, i++)
                buffer[pos] = bytes[i];

            pipe.Write(buffer, 0, buffer.Length);
        }

        public void Dispose()
        {
            //Upon disposing the session the thread will end
            TraceEventSession?.Dispose();
            pipe?.Dispose();
            disposing = true;

            if (!HasExited)
            {
                try
                {
                    Process.Kill();
                }
                finally
                {
                }
            }
        }
    }
}
