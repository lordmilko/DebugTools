using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using ClrDebug;
using DebugTools.SOS;
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

        public ConcurrentDictionary<long, IMethodInfo> Methods { get; } = new ConcurrentDictionary<long, IMethodInfo>();

        public bool HasExited => Process?.HasExited ?? true;

        private NamedPipeClientStream pipe;

        public Dictionary<int, ThreadStack> ThreadCache { get; } = new Dictionary<int, ThreadStack>();
        private Dictionary<int, string> threadNames = new Dictionary<int, string>();

        private bool collectStackTrace;
        private bool stopping;
        private DateTime stopTime;

        private CancellationTokenSource traceCTS;
        private CancellationTokenSource userCTS;

        private DateTime lastEvent;
        private Thread cancelThread;

        private Exception threadProcException;
        private BlockingCollection<IFrame> watchQueue;

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

            parser.CallEnter += Parser_CallEnter;
            parser.CallLeave += Parser_CallLeave;
            parser.Tailcall += Parser_Tailcall;

            parser.CallEnterDetailed += Parser_CallEnterDetailed;
            parser.CallLeaveDetailed += Parser_CallLeaveDetailed;
            parser.TailcallDetailed += Parser_TailcallDetailed;

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
                //Calling Dispose() guarantees an immediate stop
                TraceEventSession.Dispose();

                traceCTS?.Cancel();
                userCTS?.Cancel();
            };

            Thread = new Thread(ThreadProc);
        }

        #region MethodInfo

        private void Parser_MethodInfo(MethodInfoArgs v)
        {
            Methods[v.FunctionID] = new MethodInfo(v.FunctionID, v.ModuleName, v.TypeName, v.MethodName);
        }

        private void Parser_MethodInfoDetailed(MethodInfoDetailedArgs v)
        {
            Methods[v.FunctionID] = new MethodInfoDetailed(v.FunctionID, v.ModuleName, v.TypeName, v.MethodName, v.Token, v.SigBlob, v.SigBlobLength);
        }

        #endregion
        #region CallArgs

        private void Parser_CallEnter(CallArgs args) =>
            CallEnterCommon(args, (t, v, m) => t.Enter(v, m));

        private void Parser_CallLeave(CallArgs v)
        {
            ProcessStopping(v.TimeStamp);

            if (collectStackTrace)
            {
                Validate(v);

                if (ThreadCache.TryGetValue(v.ThreadID, out var threadStack))
                    threadStack.Leave(v, GetMethodSafe(v.FunctionID));
            }
        }

        private void Parser_Tailcall(CallArgs v)
        {
            ProcessStopping(v.TimeStamp);

            if (collectStackTrace)
            {
                Validate(v);

                if (ThreadCache.TryGetValue(v.ThreadID, out var threadStack))
                    threadStack.Tailcall(v, GetMethodSafe(v.FunctionID));
            }
        }

        private void CallEnterCommon<T>(T args, Func<ThreadStack, T, IMethodInfo, IFrame> addMethod) where T : ICallArgs
        {
            ProcessStopping(args.TimeStamp);

            if (collectStackTrace)
            {
                Validate(args);

                bool setName = false;

                if (!ThreadCache.TryGetValue(args.ThreadID, out var threadStack))
                {
                    threadStack = new ThreadStack();
                    ThreadCache[args.ThreadID] = threadStack;

                    setName = true;
                }

                var method = GetMethodSafe(args.FunctionID);
                var frame = addMethod(threadStack, args, method);

                var local = watchQueue;
                local?.Add(frame);

                if (setName && threadNames.TryGetValue(args.ThreadID, out var name))
                    threadStack.Root.ThreadName = name;
            }
        }

        #endregion
        #region CallDetailedArgs

        private void Parser_CallEnterDetailed(CallDetailedArgs args) => CallEnterCommon(args, (t, v, m) =>
        {
            Validate(v);

            return t.EnterDetailed(v, m);
        });

        private void Parser_CallLeaveDetailed(CallDetailedArgs args)
        {
            ProcessStopping(args.TimeStamp);

            if (collectStackTrace)
            {
                Validate(args);

                if (ThreadCache.TryGetValue(args.ThreadID, out var threadStack))
                    threadStack.LeaveDetailed(args, GetMethodSafe(args.FunctionID));
            }
        }

        private void Parser_TailcallDetailed(CallDetailedArgs args)
        {
            ProcessStopping(args.TimeStamp);

            if (collectStackTrace)
            {
                Validate(args);

                if (ThreadCache.TryGetValue(args.ThreadID, out var threadStack))
                    threadStack.TailcallDetailed(args, GetMethodSafe(args.FunctionID));
            }
        }

        private void Validate(ICallArgs args)
        {
            if ((uint) args.HRESULT == (uint) PROFILER_HRESULT.PROFILER_E_UNKNOWN_FRAME)
                throw new ProfilerException("Profiler encountered an unexpected function while processing a Leave/Tailcall. Current stack frame is unknown, profiler cannot continue.", (PROFILER_HRESULT) args.HRESULT);

            if ((uint) args.HRESULT >= 0x80041001 && (uint) args.HRESULT <= 0x80042000)
            {
                return;
                //throw new ProfilerException((PROFILER_HRESULT)(uint)args.HRESULT);
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
        #region Exception

        private void Parser_Exception(ExceptionArgs args)
        {
            if (collectStackTrace)
            {
                if (ThreadCache.TryGetValue(args.ThreadID, out var threadStack))
                    threadStack.Exception(args);
            }
        }

        private void Parser_ExceptionFrameUnwind(CallArgs v)
        {
            ProcessStopping(v.TimeStamp);

            if (collectStackTrace)
            {
                Validate(v);

                if (ThreadCache.TryGetValue(v.ThreadID, out var threadStack))
                    threadStack.ExceptionFrameUnwind(v, GetMethodSafe(v.FunctionID));
            }
        }

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

        private IMethodInfo GetMethodSafe(long functionId)
        {
            if (Methods.TryGetValue(functionId, out var value))
                return value;

            value = new MethodInfo(functionId, "Unknown", "Unknown", "Unknown");
            Methods[functionId] = value;
            return value;
        }

        public void Start(CancellationToken cancellationToken, string processName, ProfilerSetting[] settings)
        {
            collectStackTrace = settings?.Any(s => s == ProfilerSetting.TraceStart) == true;

            var pipeCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            traceCTS = new CancellationTokenSource();

            Process = ProfilerInfo.CreateProcess(processName, p =>
            {
                //If we're only targeting a specific child process, we can't filter based on the process we just created
                 TraceEventProviderOptions options = settings?.Any(s => s.Flag == ProfilerEnvFlags.TargetProcess) == true
                     ? null
                     : new TraceEventProviderOptions {ProcessIDFilter = new[] {p.Id}};

                TraceEventSession.EnableProvider(ProfilerTraceEventParser.ProviderGuid, options: options);

                Thread.Start();

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
                        cancelThread = new Thread(CancelTraceThreadProc);

                        cancelThread.Start();
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

        private void CancelTraceThreadProc()
        {
            lastEvent = DateTime.Now;

            while (true)
            {
                //If we go more than 2 seconds without receiving a new event after the process has shutdown, we'll assume no more events are incoming
                var threshold = DateTime.Now.AddSeconds(-2);

                if (threshold > lastEvent)
                {
                    traceCTS.Cancel();
                    break;
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
            if (Process.HasExited)
                return;

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
                if (!Process.HasExited)
                    ExecuteCommand(MessageType.EnableTracing, false);
            }
        }

        private IEnumerable<IFrame> WithTracing(Func<IEnumerable<IFrame>> action)
        {
            if (Process.HasExited)
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
                if (!Process.HasExited)
                    ExecuteCommand(MessageType.EnableTracing, false);
            }
        }

        public void ThrowOnError()
        {
            if (threadProcException != null)
                throw threadProcException;
        }

        public void ExecuteCommand(MessageType messageType, object value)
        {
            if (pipe == null)
                return;

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
