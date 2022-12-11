using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using DebugTools.Tracing;
using Microsoft.Diagnostics.Tracing.Session;

namespace DebugTools.PowerShell
{
    public class ProfilerSession : IDisposable
    {
        private static int maxId;

        public Process Process { get; private set; }

        public TraceEventSession TraceEventSession { get; }

        public Thread Thread { get; }

        public ConcurrentBag<MethodInfo> Methods { get; } = new ConcurrentBag<MethodInfo>();

        public bool HasExited => Process.HasExited;

        public ProfilerSession()
        {
            var sessionName = GetNextSessionName();

            TryCloseSession(sessionName);

            TraceEventSession = new TraceEventSession(sessionName);

            var parser = new ProfilerTraceEventParser(TraceEventSession.Source);

            parser.MethodInfo += v =>
            {
                Methods.Add(new MethodInfo(v.FunctionID, v.ModuleName, v.TypeName, v.MethodName));
            };

            TraceEventSession.EnableProvider(ProfilerTraceEventParser.ProviderGuid);

            Thread = new Thread(ThreadProc);
        }

        public void Start(ProcessStartInfo psi)
        {
            Thread.Start();

            Process = Process.Start(psi);
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

        public void Dispose()
        {
            //Upon disposing the session the thread will end
            TraceEventSession?.Dispose();
        }
    }
}