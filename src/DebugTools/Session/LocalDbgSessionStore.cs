using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ClrDebug;
using DebugTools.Host;
using Architecture = DebugTools.Host.Architecture;

namespace DebugTools
{
    class LocalDbgSessionStore : DbgSessionStore
    {
        private HostInfo Hostx86;
        private HostInfo Hostx64;

        internal HostApp GetOptionalHost(Process targetProcess) =>
            GetOptionalHostInfo(targetProcess)?.Host;

        private HostInfo GetOptionalHostInfo(Process targetProcess)
        {
            if (!NativeMethods.IsWow64Process(targetProcess.Handle, out var isWow64))
                throw new InvalidOperationException($"Failed to query {nameof(NativeMethods.IsWow64Process)}: {(HRESULT)Marshal.GetHRForLastWin32Error()}");

            if (isWow64)
            {
                if (Hostx86.Host == null || Hostx86.Process.HasExited)
                    return null;

                Hostx86.AddUser(targetProcess);
                return Hostx86;
            }
            else
            {
                if (Hostx64.Host == null || Hostx64.Process.HasExited)
                    return null;

                Hostx64.AddUser(targetProcess);
                return Hostx64;
            }
        }

        internal HostApp GetDetectedHost(Process targetProcess, bool needDebug = false) =>
            GetDetectedHostInfo(targetProcess, needDebug).Host;

        internal HostInfo GetDetectedHostInfo(Process targetProcess, bool needDebug = false)
        {
            if (!NativeMethods.IsWow64Process(targetProcess.Handle, out var isWow64))
                throw new InvalidOperationException($"Failed to query {nameof(NativeMethods.IsWow64Process)}: {(HRESULT)Marshal.GetHRForLastWin32Error()}");

            if (isWow64)
            {
                if (Hostx86 == null || Hostx86.Process.HasExited)
                {
                    var hostApp = HostProvider.CreateApp(Architecture.x86, needDebug);
                    var hostProcess = Process.GetProcessById(hostApp.ProcessId);

                    Hostx86 = new HostInfo(hostApp, hostProcess);
                }

                TryDebug(Hostx86, needDebug);

                Hostx86.AddUser(targetProcess);
                return Hostx86;
            }
            else
            {
                if (Hostx64 == null || Hostx64.Process.HasExited)
                {
                    var hostApp = HostProvider.CreateApp(Architecture.x64, needDebug);
                    var hostProcess = Process.GetProcessById(hostApp.ProcessId);

                    Hostx64 = new HostInfo(hostApp, hostProcess);
                }

                TryDebug(Hostx64, needDebug);

                Hostx64.AddUser(targetProcess);
                return Hostx64;
            }
        }

        private static void TryDebug(HostInfo host, bool needDebug)
        {
            if (!needDebug || host.Host.IsDebuggerAttached || Process.GetCurrentProcess().Id == host.Process.Id)
                return;

            VsDebugger.Attach(host.Process, "Managed");
            host.Host.IsDebuggerAttached = true;
        }

        public void Close(int processId, DbgServiceType serviceType, object service)
        {
            if (sessions.TryGetValue(processId, out var session))
            {
                if (session.Contains(service))
                {
                    session.Close(service);

                    if (service is IHostAppSession c)
                    {
                        var host = c.HostApp;

                        if (host != null)
                        {
                            host.DisposeService(new DbgSessionHandle(processId), serviceType);

                            if (ReferenceEquals(Hostx86?.Host, host))
                            {
                                if (Hostx86.Release(processId))
                                    Hostx86 = null;
                            }
                            else if (ReferenceEquals(Hostx64?.Host, host))
                            {
                                if (Hostx64.Release(processId))
                                    Hostx64 = null;
                            }
                        }
                    }
                }
            }
        }
    }
}
