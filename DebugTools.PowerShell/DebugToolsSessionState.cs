using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using ClrDebug;
using DebugTools.Host;
using DebugTools.Profiler;
using DebugTools.SOS;
using Architecture = DebugTools.Host.Architecture;

namespace DebugTools.PowerShell
{
    static class DebugToolsSessionState
    {
        internal static List<ProfilerSession> ProfilerSessions = new List<ProfilerSession>();

        internal static List<SOSProcess> SOSProcesses = new List<SOSProcess>();

        internal static (HostApp host, Process process) Hostx86;
        internal static (HostApp host, Process process) Hostx64;

        internal static ProfilerSession GetImplicitProfilerSession()
        {
            //Check for alive

            var sessions = ProfilerSessions.Where(i => !i.Process.HasExited).ToArray();

            if (sessions.Length == 1)
                return sessions[0];

            if (sessions.Length > 1)
                throw new InvalidOperationException("Cannot execute cmdlet: no -Session was specified and more than one Session belonging to active processes was found in the PowerShell session.");

            //Check for dead

            if (ProfilerSessions.Count == 1)
                return ProfilerSessions[0];

            if (ProfilerSessions.Count > 0)
                return ProfilerSessions.Last(); //All of the sessions have ended, so take the last one

            throw new InvalidOperationException($"Cannot execute cmdlet: no -Session was specified and no global Session could be found in the PowerShell session.");
        }

        internal static SOSProcess GetImplicitSOSProcess()
        {
            //Check for alive

            var processes = SOSProcesses.Where(i => !i.Process.HasExited).ToArray();

            if (processes.Length == 1)
                return processes[0];

            if (processes.Length > 1)
                throw new InvalidOperationException($"Cannot execute cmdlet: no -Process was specified and more than one Process belonging to active processes was found in the PowerShell session.");

            //Check for dead

            if (SOSProcesses.Count > 0)
                throw new InvalidOperationException($"Cannot execute cmdlet: no -Process was specified and all previous SOS Processes have now terminated.");

            throw new InvalidOperationException($"Cannot execute cmdlet: no -Session was specified and no global Session could be found in the PowerShell session.");
        }

        internal static HostApp GetDetectedHost(Process targetProcess, bool needDebug = false)
        {
            if (!NativeMethods.IsWow64Process(targetProcess.Handle, out var isWow64))
                throw new SOSException($"Failed to query {nameof(NativeMethods.IsWow64Process)}: {(HRESULT)Marshal.GetHRForLastWin32Error()}");

            if (isWow64)
            {
                if (Hostx86.host == null || Hostx86.process.HasExited)
                {
                    var hostApp = HostProvider.CreateApp(Architecture.x86, needDebug);
                    var hostProcess = Process.GetProcessById(hostApp.ProcessId);

                    Hostx86 = (hostApp, hostProcess);
                }

                TryDebug(Hostx86, needDebug);
                
                return Hostx86.host;
            }
            else
            {
                if (Hostx64.host == null || Hostx64.process.HasExited)
                {
                    var hostApp = HostProvider.CreateApp(Architecture.x64, needDebug);
                    var hostProcess = Process.GetProcessById(hostApp.ProcessId);

                    Hostx64 = (hostApp, hostProcess);
                }

                TryDebug(Hostx86, needDebug);

                return Hostx64.host;
            }
        }

        private static void TryDebug((HostApp host, Process process) host, bool needDebug)
        {
            if (!needDebug || host.host.IsDebuggerAttached)
                return;

            VsDebugger.Attach(host.process, "Managed");
            host.host.IsDebuggerAttached = true;
        }
    }
}
