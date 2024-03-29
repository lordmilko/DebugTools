﻿using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using ChaosLib;
using ClrDebug;

namespace DebugTools.Profiler
{
    public class ProfilerInfo
    {
        public static readonly string Profilerx86;
        public static readonly string Profilerx64;
        public static readonly string Nativex86;
        public static readonly string Nativex64;
        public static readonly string DebugHostx86;
        public static readonly string DebugHostx64;
        public static readonly string TestHost;

        internal const string DebugHostName = "DebugTools.Host.{0}.exe";

        public static readonly string InstallationRoot;

        public static readonly Guid Guid = new Guid("9FA9EA80-BE5D-419E-A667-15A672CBD280");

        static ProfilerInfo()
        {
            var dll = new Uri(typeof(ProfilerInfo).Assembly.CodeBase);
            var root = dll.Host + dll.PathAndQuery + dll.Fragment;
            var rootStr = Uri.UnescapeDataString(root);

            InstallationRoot = Path.GetDirectoryName(rootStr);

            var profilerName = "Profiler.{0}.dll";
            var nativeName = "DebugTools.Native.{0}.dll";

            Profilerx86 = Path.Combine(InstallationRoot, "x86", string.Format(profilerName, "x86"));
            Profilerx64 = Path.Combine(InstallationRoot, "x64", string.Format(profilerName, "x64"));
            Nativex86 = Path.Combine(InstallationRoot, "x86", string.Format(nativeName, "x86"));
            Nativex64 = Path.Combine(InstallationRoot, "x64", string.Format(nativeName, "x64"));
            DebugHostx86 = Path.Combine(InstallationRoot, string.Format(DebugHostName, "x86"));
            DebugHostx64 = Path.Combine(InstallationRoot, string.Format(DebugHostName, "x64"));

            if (!File.Exists(Profilerx86) && !File.Exists(Profilerx64))
            {
                //Maybe it's a unit test; look inside the DebugTools folder instead
                Profilerx86 = Path.Combine(InstallationRoot, "DebugTools", "x86", string.Format(profilerName, "x86"));
                Profilerx64 = Path.Combine(InstallationRoot, "DebugTools", "x64", string.Format(profilerName, "x64"));
                Nativex86 = Path.Combine(InstallationRoot, "DebugTools", "x86", string.Format(nativeName, "x86"));
                Nativex64 = Path.Combine(InstallationRoot, "DebugTools", "x64", string.Format(nativeName, "x64"));
                DebugHostx86 = Path.Combine(InstallationRoot, "DebugTools", string.Format(DebugHostName, "x86"));
                DebugHostx64 = Path.Combine(InstallationRoot, "DebugTools", string.Format(DebugHostName, "x64"));
            }

            if (!File.Exists(Profilerx86))
                Profilerx86 = null;

            if (!File.Exists(Profilerx64))
                Profilerx64 = null;

            if (Profilerx86 == null && Profilerx64 == null)
                throw new InvalidOperationException("No profiler DLLs could be found.");

            if (!File.Exists(Nativex86))
                Nativex86 = null;

            if (!File.Exists(Nativex64))
                Nativex64 = null;

            if (Nativex86 == null && Nativex64 == null)
                throw new InvalidOperationException("No native DLLs could be found.");

            TestHost = Path.Combine(InstallationRoot, "DebugTools.TestHost.exe");
        }

        public static Process CreateProcess(string processName, Action<Process> startCallback, ProfilerSetting[] settings)
        {
            var envVariables = new StringDictionary
            {
                { "COR_ENABLE_PROFILING", "1" },
                { "COR_PROFILER", Guid.ToString("B") },

                { "CORECLR_ENABLE_PROFILING", "1" },
                { "CORECLR_PROFILER", Guid.ToString("B") },

                { "DEBUGTOOLS_PARENT_PID", Process.GetCurrentProcess().Id.ToString() }
            };

            if (Profilerx86 != null)
            {
                envVariables["COR_PROFILER_PATH_32"] = Profilerx86;
                envVariables["CORECLR_PROFILER_PATH_32"] = Profilerx86;
            }

            if (Profilerx64 != null)
            {
                envVariables["COR_PROFILER_PATH_64"] = Profilerx64;
                envVariables["CORECLR_PROFILER_PATH_64"] = Profilerx64;
            }

            bool needDebug = false;
            bool ignoreDefaultBlacklist = false;
            bool minimized = false;

            if (settings != null)
            {
                foreach (var setting in settings)
                {
                    switch (setting.Flag)
                    {
                        case ProfilerEnvFlags.WaitForDebugger:
                            //If we're not being debugged, there's no point trying to tell the target process that it needs to be debugged as well
                            if (Debugger.IsAttached)
                            {
                                needDebug = true;
                                envVariables.Add("DEBUGTOOLS_WAITFORDEBUG", "1");
                            }

                            break;

                        case ProfilerEnvFlags.Detailed:
                            envVariables.Add("DEBUGTOOLS_DETAILED", "1");
                            break;

                        case ProfilerEnvFlags.TraceStart:
                            envVariables.Add("DEBUGTOOLS_TRACESTART", "1");
                            break;

                        case ProfilerEnvFlags.TraceValueDepth:
                            envVariables.Add("DEBUGTOOLS_TRACEVALUEDEPTH", setting.StringValue);
                            break;

                        case ProfilerEnvFlags.TargetProcess:
                            envVariables.Add("DEBUGTOOLS_TARGET_PROCESS", setting.StringValue);
                            break;

                        case ProfilerEnvFlags.ModuleBlacklist:
                            envVariables.Add("DEBUGTOOLS_MODULEBLACKLIST", setting.StringValue);
                            break;

                        case ProfilerEnvFlags.ModuleWhitelist:
                            envVariables.Add("DEBUGTOOLS_MODULEWHITELIST", setting.StringValue);
                            break;

                        case ProfilerEnvFlags.IgnorePointerValue:
                            envVariables.Add("DEBUGTOOLS_IGNORE_POINTERVALUE", "1");
                            break;

                        case ProfilerEnvFlags.IgnoreDefaultBlacklist:
                            ignoreDefaultBlacklist = true;
                            break;

                        case ProfilerEnvFlags.SynchronousTransfers:
                            envVariables.Add("DEBUGTOOLS_SYNCHRONOUS_TRANSFERS", "1");
                            break;

                        case ProfilerEnvFlags.Minimized:
                            minimized = true;
                            break;

                        case ProfilerEnvFlags.DisablePipe:
                        case ProfilerEnvFlags.IncludeUnknownUnmanagedTransitions:
                            break;

                        default:
                            throw new NotImplementedException($"Don't know how to handle flag '{setting.Flag}'.");
                    }
                }
            }

            if (ignoreDefaultBlacklist)
                envVariables.Add("DEBUGTOOLS_IGNORE_DEFAULT_BLACKLIST", "1");

            STARTUPINFOA si = new STARTUPINFOA
            {
                cb = Marshal.SizeOf<STARTUPINFOA>(),
            };

            if (minimized)
            {
                si.dwFlags = STARTF.STARTF_USESHOWWINDOW;
                si.wShowWindow = ShowWindow.Minimized;
            }

            //You MUST ensure all global environment variables are defined; otherwise certain programs that assume these variables exist (such as PowerShell) may crash
            foreach (DictionaryEntry environmentVariable in Environment.GetEnvironmentVariables())
                envVariables.Add((string)environmentVariable.Key, (string)environmentVariable.Value);

            var envHandle = GCHandle.Alloc(GetEnvironmentBytes(envVariables), GCHandleType.Pinned);
            var envPtr = envHandle.AddrOfPinnedObject();

            try
            {
                Kernel32.CreateProcessA(
                    processName,
                    CreateProcessFlags.CREATE_NEW_CONSOLE | CreateProcessFlags.CREATE_SUSPENDED,
                    envPtr,
                    Environment.CurrentDirectory,
                    ref si,
                    out var pi
                );

                var process = Process.GetProcessById(pi.dwProcessId);

                startCallback?.Invoke(process);

                Kernel32.ResumeThread(pi.hThread);

                Kernel32.CloseHandle(pi.hProcess);
                Kernel32.CloseHandle(pi.hThread);

                if (needDebug)
                    VsDebugger.Attach(process, VsDebuggerType.Native);

                return process;
            }
            finally
            {
                envHandle.Free();
            }
        }

        internal static byte[] GetEnvironmentBytes(StringDictionary sd)
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
    }
}
