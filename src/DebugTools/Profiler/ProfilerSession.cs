using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ClrDebug;

namespace DebugTools.Profiler
{
    public partial class ProfilerSession : IDisposable
    {
        public virtual int? PID => Target.ProcessId;

        public virtual string Name => Target.Name;

        public ProfilerSessionType Type { get; }

        private ProfilerSessionStatus? status;

        public ProfilerSessionStatus Status
        {
            get
            {
                if (status != null)
                    return status.Value;

                if (!Target.IsAlive)
                    return ProfilerSessionStatus.Exited;

                return ProfilerSessionStatus.Active;
            }
            internal set { status = value; }
        }

        public IProfilerTarget Target { get; set; }

        public Thread Thread { get; }

        public bool IsAlive => Target.IsAlive;

        internal ConcurrentDictionary<long, IMethodInfoInternal> Methods { get; } = new ConcurrentDictionary<long, IMethodInfoInternal>();

        //Only populated in detailed mode
        internal ConcurrentDictionary<int, ModuleInfo> Modules { get; } = new ConcurrentDictionary<int, ModuleInfo>();

        public Dictionary<int, ThreadStack> ThreadCache { get; } = new Dictionary<int, ThreadStack>();
        private Dictionary<int, int> threadIdToSequenceMap = new Dictionary<int, int>();
        private Dictionary<int, int> threadSequenceToIdMap = new Dictionary<int, int>();
        private Dictionary<int, string> threadNames = new Dictionary<int, string>(); //Sequence -> Name

        private bool collectStackTrace;
        private bool includeUnknownTransitions;
        private bool stopping;
        private bool cancelIfTimeoutNoEvents;
        private DateTime stopTime;
        private bool isDebugged;

        private CancellationTokenSource traceCTS;
        private CancellationTokenSource userCTS;

        private DateTime lastEvent;
        private Thread cancelThread;

        private Exception threadProcException;
        private BlockingCollection<IFrame> watchQueue;
        private bool global;
        private bool disposing;
        private object staticFieldLock = new object();
        private Either<object, HRESULT> staticFieldValue;
        private AutoResetEvent staticFieldValueEvent = new AutoResetEvent(false);

        public ThreadStack[] LastTrace { get; internal set; }

        internal IProfilerReader Reader { get; }

        public ProfilerSession(IProfilerReaderConfig config)
        {
            Type = config.SessionType;

            switch (config.SessionType)
            {
                case ProfilerSessionType.EtwFile:
                    Reader = new FileEtwProfilerReader((FileEtwProfilerReaderConfig) config);
                    break;

                case ProfilerSessionType.MMF:
                    Reader = new MemoryMappedFileProfilerReader((LiveProfilerReaderConfig) config);
                    break;

                default:
                    Reader = new LiveEtwProfilerReader((LiveProfilerReaderConfig) config);
                    break;
            }

            SetEventHandlers();

            Thread = new Thread(ThreadProc) {Name = "ETWThreadProc"};
            cancelThread = new Thread(CancelTraceThreadProc) {Name = "CancelThreadProc"};
        }

        public void Start(CancellationToken cancellationToken)
        {
            var settings = Reader.Config.Settings;

            collectStackTrace = settings?.Any(s => s == ProfilerSetting.TraceStart) == true;
            includeUnknownTransitions = settings?.Any(s => s == ProfilerSetting.IncludeUnknownUnmanagedTransitions) == true;

            traceCTS = new CancellationTokenSource();
            isDebugged = Debugger.IsAttached && settings?.Any(s => s.Flag == ProfilerEnvFlags.WaitForDebugger) == true;

            Target = Reader.CreateTarget();

            Target.Start(() =>
            {
                Reader.Initialize();

                Thread.Start();
                cancelThread.Start();

                if (traceCTS != null)
                {
                    cancelIfTimeoutNoEvents = false;
                }

                Target.SetExitHandler(OnTargetExit);
            }, cancellationToken);
        }

        private void OnTargetExit(bool isGracefulExit)
        {
            userCTS?.Cancel();
            traceCTS?.Token.Register(() => Reader.Stop());

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
                if (!isGracefulExit)
                {
                    Thread.Sleep(3000);
                    Reader.Stop();
                }
            }
        }

        public void StartGlobal()
        {
            global = true;
            traceCTS = new CancellationTokenSource();

            Reader.InitializeGlobal();

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

            //If we haven't already cancelled, do so now
            traceCTS?.Cancel();
            cancelIfTimeoutNoEvents = false;
        }

        private void ThreadProc()
        {
            try
            {
                threadProcException = null;
                Reader.Execute();

                Debug.WriteLine("########## ETW THREADPROC ENDED ##########");
            }
            catch(Exception ex)
            {
                threadProcException = ex;
                Reader.Stop();
                userCTS?.Cancel();
                traceCTS?.Cancel();
            }
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
            if (threadProcException != null)
                throw new InvalidOperationException("Cannot trace as ETW ThreadProc has already terminated due to previous exception.", threadProcException);

            if (Target != null && !Target.IsAlive)
                return;

            if (traceCTS.IsCancellationRequested)
                traceCTS = new CancellationTokenSource();

            ThreadCache.Clear();

            if (Target != null && Target.IsAlive)
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
                if (Target != null && Target.IsAlive)
                    ExecuteCommand(MessageType.EnableTracing, false);
            }
        }

        private IEnumerable<IFrame> WithTracing(Func<IEnumerable<IFrame>> action)
        {
            if (Target?.IsAlive == false)
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
                if (Target != null && Target.IsAlive)
                    ExecuteCommand(MessageType.EnableTracing, false);
            }
        }

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

        internal IMethodInfoInternal GetMethodSafe(long functionId)
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

        public object GetStaticField(string name, int threadId = 0, int maxTraceDepth = 0)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException($"A type and name must be specified however was an empty string was provided.", nameof(name));

            if (name.IndexOf('.') == -1)
                throw new ArgumentException("A type and name should be specified, separated by a period. e.g. 'Foo.bar'", nameof(name));

            lock (staticFieldLock)
            {
                ExecuteCommand(MessageType.GetStaticField, $"{name}|{threadId}|{maxTraceDepth}");

                if (!staticFieldValueEvent.WaitOne(isDebugged ? -1 : (int) TimeSpan.FromSeconds(5).TotalMilliseconds))
                    throw new TimeoutException("Timed out waiting for profiler to read static field");

                var value = staticFieldValue;

                if (value.IsLeft)
                    return value.Left;

                if (value.Right.IsProfilerHRESULT())
                    throw new ProfilerException((PROFILER_HRESULT) value.Right);

                value.Right.ThrowOnNotOK();
            }

            throw new InvalidOperationException("This code should be unreachable.");
        }

        public void ExecuteCommand(MessageType messageType, object value) =>
            Target.ExecuteCommand(messageType, value);

        public void Dispose()
        {
            //Upon disposing the session the thread will end
            Reader?.Dispose();
            disposing = true;

            Target?.Dispose();
        }
    }
}
