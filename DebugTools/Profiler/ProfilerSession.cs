using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using DebugTools.Tracing;
using Microsoft.Diagnostics.Tracing.Session;

namespace DebugTools.Profiler
{
    public class ProfilerSession : IDisposable
    {
        private static int maxId;

        public Process Process { get; private set; }

        public TraceEventSession TraceEventSession { get; }

        public Thread Thread { get; }

        public ConcurrentDictionary<long, MethodInfo> Methods { get; } = new ConcurrentDictionary<long, MethodInfo>();

        public bool HasExited => Process.HasExited;

        private NamedPipeClientStream pipe;

        private Dictionary<int, ThreadStack> threadCache = new Dictionary<int, ThreadStack>();
        private Dictionary<int, string> threadNames = new Dictionary<int, string>();

        private bool collectStackTrace;
        private bool stopping;
        private DateTime stopTime;

        private CancellationTokenSource traceCTS;

        public ThreadStack[] LastTrace { get; private set; }

        public ProfilerSession()
        {
            var sessionName = GetNextSessionName();

            TryCloseSession(sessionName);

            TraceEventSession = new TraceEventSession(sessionName);

            var parser = new ProfilerTraceEventParser(TraceEventSession.Source);

            parser.MethodInfo += v =>
            {
                Methods[v.FunctionID] = new MethodInfo(v.FunctionID, v.ModuleName, v.TypeName, v.MethodName);
            };

            parser.CallEnter += v =>
            {
                if (stopping && v.TimeStamp > stopTime)
                {
                    collectStackTrace = false;
                    stopping = false;
                    traceCTS.Cancel();
                }

                if (collectStackTrace)
                {
                    bool setName = false;

                    if (!threadCache.TryGetValue(v.ThreadID, out var threadStack))
                    {
                        threadStack = new ThreadStack();
                        threadCache[v.ThreadID] = threadStack;

                        setName = true;
                    }

                    threadStack.AddMethod(v, GetMethodSafe(v.FunctionID));

                    if (setName && threadNames.TryGetValue(v.ThreadID, out var name))
                        threadStack.Root.ThreadName = name;
                }
            };

            parser.CallExit += v =>
            {
                if (stopping && v.TimeStamp > stopTime)
                {
                    collectStackTrace = false;
                    stopping = false;
                    traceCTS.Cancel();
                }

                if (collectStackTrace)
                {
                    if (threadCache.TryGetValue(v.ThreadID, out var threadStack))
                        threadStack.EndCall();
                }
            };

            parser.Tailcall += v =>
            {
                if (stopping && v.TimeStamp > stopTime)
                {
                    collectStackTrace = false;
                    stopping = false;
                    traceCTS.Cancel();
                }

                if (collectStackTrace)
                {
                    if (threadCache.TryGetValue(v.ThreadID, out var threadStack))
                        threadStack.Tailcall(v, GetMethodSafe(v.FunctionID));
                }
            };

            parser.ThreadName += v =>
            {
                threadNames[v.ThreadID] = v.ThreadName;

                if (threadCache.TryGetValue(v.ThreadID, out var stack))
                    stack.Root.ThreadName = v.ThreadName;
            };

            TraceEventSession.EnableProvider(ProfilerTraceEventParser.ProviderGuid);

            Thread = new Thread(ThreadProc);
        }

        private MethodInfo GetMethodSafe(long functionId)
        {
            if (Methods.TryGetValue(functionId, out var value))
                return value;

            value = new MethodInfo(functionId, "Unknown", "Unknown", "Unknown");
            Methods[functionId] = value;
            return value;
        }

        public void Start(CancellationToken cancellationToken, string processName, ProfilerEnvFlags[] flags, bool traceStart)
        {
            collectStackTrace = traceStart;

            Thread.Start();

            Process = ProfilerInfo.CreateProcess(processName, flags);

            pipe = new NamedPipeClientStream(".", $"DebugToolsProfilerPipe_{Process.Id}", PipeDirection.Out);

            //This will wait for the pipe to be created if it doesn't exist yet
            //wait async, with a cancellation token
            pipe.ConnectAsync(10000, cancellationToken).GetAwaiter().GetResult();
        }

        private void ThreadProc()
        {
            TraceEventSession.Source.Process();
        }

        private void TryCloseSession(string sessionName)
        {
            TraceEventSession.GetActiveSession(sessionName)?.Dispose();
        }

        private string GetNextSessionName()
        {
            return "DebugTools_Profiler_" + ++maxId;
        }

        public ThreadStack[] Trace(CancellationToken cancellationToken)
        {
            traceCTS = new CancellationTokenSource();

            collectStackTrace = true;

            cancellationToken.Register(() =>
            {
                stopTime = DateTime.Now;
                stopping = true;
                Console.WriteLine("Stopping...");
            });

            cancellationToken.WaitHandle.WaitOne();

            traceCTS.Token.WaitHandle.WaitOne();

            LastTrace = threadCache.Values.ToArray();

            threadCache.Clear();

            return LastTrace;
        }

        public void ExecuteCommand(MessageType messageType, object value)
        {
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