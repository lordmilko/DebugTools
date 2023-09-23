using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using ChaosLib;
using ClrDebug;
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
                VsDebugger.Attach(process, VsDebuggerType.Managed);

            var eventName = $"{string.Format(ProfilerInfo.DebugHostName.Substring(0, ProfilerInfo.DebugHostName.LastIndexOf('.')), architecture.ToString())}_{process.Id}";

            var waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, eventName);

            //Wait for the IPC server to start (see RunIPCServer())
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
            STARTUPINFOA si = new STARTUPINFOA
            {
                cb = Marshal.SizeOf<STARTUPINFOA>(),
                dwFlags = STARTF.STARTF_USESHOWWINDOW,
                wShowWindow = ShowWindow.Hide
            };

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

            Kernel32.CreateProcessA(
                architecture == Architecture.x86 ? ProfilerInfo.DebugHostx86 : ProfilerInfo.DebugHostx64,
                CreateProcessFlags.CREATE_NEW_CONSOLE | CreateProcessFlags.CREATE_SUSPENDED,
                envPtr,
                Environment.CurrentDirectory,
                ref si,
                out var pi
            );

            var process = Process.GetProcessById(pi.dwProcessId);

            Kernel32.ResumeThread(pi.hThread);

            Kernel32.CloseHandle(pi.hProcess);
            Kernel32.CloseHandle(pi.hThread);

            return process;
        }
    }
}
