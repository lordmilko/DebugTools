using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using DebugTools.Profiler;

namespace DebugTools.Host
{
    public enum Architecture
    {
        x86,
        x64
    }

    class HostProvider
    {
        internal static HostApp CreateApp(Architecture architecture, bool needDebug)
        {
            if ((architecture == Architecture.x86 && IntPtr.Size == 4) ||
                (architecture == Architecture.x64 && IntPtr.Size == 8))
            {
                return new HostApp();
            }

            var process = StartProcess(architecture, needDebug);

            if (needDebug)
                VsDebugger.Attach(process, "Managed");

            var eventName = $"{string.Format(ProfilerInfo.DebugHostName.Substring(0, ProfilerInfo.DebugHostName.LastIndexOf('.')), architecture.ToString())}_{process.Id}";

            var waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, eventName);

            waitHandle.WaitOne();

            var ipcClient = new IpcClient();
            ipcClient.Connect(process);

            var remoteExecutor = RemoteExecutor.GetInstanceFromRemoteProcess(process);

            var app = remoteExecutor.ExecuteInRemoteProcess<HostApp>(typeof(HostApp), nameof(HostApp.GetInstance));

            app.IsDebuggerAttached = needDebug;

            return app;
        }

        private static Process StartProcess(Architecture architecture, bool needDebug)
        {
            SECURITY_ATTRIBUTES processAttribs = new SECURITY_ATTRIBUTES();
            SECURITY_ATTRIBUTES threadAttribs = new SECURITY_ATTRIBUTES();

            STARTUPINFO si = new STARTUPINFO
            {
                cb = Marshal.SizeOf<STARTUPINFO>(),
                dwFlags = STARTF.STARTF_USESHOWWINDOW,
                wShowWindow = ShowWindow.Hide
            };

            PROCESS_INFORMATION pi;

            var envVariables = new StringDictionary
            {
                {HostApp.ParentPID, Process.GetCurrentProcess().Id.ToString()}
            };

            if (needDebug)
                envVariables.Add(HostApp.WaitForDebug, "1");

            //You MUST ensure all global environment variables are defined; otherwise certain programs that assume these variables exist (such as PowerShell) may crash
            foreach (DictionaryEntry environmentVariable in Environment.GetEnvironmentVariables())
                envVariables.Add((string)environmentVariable.Key, (string)environmentVariable.Value);

            var envHandle = GCHandle.Alloc(ProfilerInfo.GetEnvironmentBytes(envVariables), GCHandleType.Pinned);
            var envPtr = envHandle.AddrOfPinnedObject();

            bool result = NativeMethods.CreateProcessA(
                null,
                architecture == Architecture.x86 ? ProfilerInfo.DebugHostx86 : ProfilerInfo.DebugHostx64,
                ref processAttribs,
                ref threadAttribs,
                true,
                CreateProcessFlags.CREATE_NEW_CONSOLE | CreateProcessFlags.CREATE_SUSPENDED,
                envPtr,
                Environment.CurrentDirectory,
                ref si,
                out pi);

            if (!result)
            {
                var err = Marshal.GetHRForLastWin32Error();

                Marshal.ThrowExceptionForHR(err);
            }

            var process = Process.GetProcessById(pi.dwProcessId);

            NativeMethods.ResumeThread(pi.hThread);

            NativeMethods.CloseHandle(pi.hProcess);
            NativeMethods.CloseHandle(pi.hThread);

            return process;
        }
    }
}
