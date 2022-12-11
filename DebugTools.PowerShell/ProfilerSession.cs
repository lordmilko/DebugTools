using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
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

        public void Start(string processName, StringDictionary envVariables)
        {
            Thread.Start();

            Process = CreateProcess(processName, envVariables);
        }

        private Process CreateProcess(string processName, StringDictionary envVariables)
        {
            SECURITY_ATTRIBUTES processAttribs = new SECURITY_ATTRIBUTES();
            SECURITY_ATTRIBUTES threadAttribs = new SECURITY_ATTRIBUTES();

            STARTUPINFO si = new STARTUPINFO();
            si.cb = Marshal.SizeOf<STARTUPINFO>();
            si.dwFlags = STARTF.STARTF_USESHOWWINDOW;
            si.wShowWindow = ShowWindow.Minimized;

            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();

            //You MUST ensure all global environment variables are defined; otherwise certain programs that assume these variables exist (such as PowerShell) may crash
            foreach (DictionaryEntry environmentVariable in Environment.GetEnvironmentVariables())
                envVariables.Add((string) environmentVariable.Key, (string) environmentVariable.Value);

            var envHandle = GCHandle.Alloc(GetEnvironmentBytes(envVariables), GCHandleType.Pinned);
            var envPtr = envHandle.AddrOfPinnedObject();

            try
            {
                bool result = NativeMethods.CreateProcessA(
                    null,
                    processName,
                    ref processAttribs,
                    ref threadAttribs,
                    true,
                    CreateProcessFlags.CREATE_NEW_CONSOLE,
                    envPtr,
                    Environment.CurrentDirectory,
                    ref si,
                    out pi);

                if (!result)
                {
                    var err = Marshal.GetHRForLastWin32Error();

                    Marshal.ThrowExceptionForHR(err);
                }

                NativeMethods.CloseHandle(pi.hProcess);
                NativeMethods.CloseHandle(pi.hThread);

                return Process.GetProcessById(pi.dwProcessId);
            }
            finally
            {
                envHandle.Free();
            }
        }

        static byte[] GetEnvironmentBytes(StringDictionary sd)
        {
            StringBuilder stringBuilder = new StringBuilder();

            var keys = new string[sd.Count];
            var values = new string[sd.Count];

            sd.Keys.CopyTo(keys, 0);
            sd.Values.CopyTo(values, 0);

            for (int index = 0; index < sd.Count; ++index)
            {
                stringBuilder.Append(keys[index]);
                stringBuilder.Append('=');
                stringBuilder.Append(values[index]);
                stringBuilder.Append(char.MinValue);
            }

            stringBuilder.Append(char.MinValue);

            return Encoding.ASCII.GetBytes(stringBuilder.ToString());
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