using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace DebugTools.Profiler
{
    public enum ProfilerEnvFlags
    {
        WaitForDebugger
    }

    public class ProfilerInfo
    {
        public static readonly string Profilerx86;
        public static readonly string Profilerx64;
        public static readonly string TestHost;

        public static readonly string InstallationRoot;

        public static readonly Guid Guid = new Guid("9FA9EA80-BE5D-419E-A667-15A672CBD280");

        static ProfilerInfo()
        {
            var dll = new Uri(typeof(ProfilerInfo).Assembly.CodeBase);
            var root = dll.Host + dll.PathAndQuery + dll.Fragment;
            var rootStr = Uri.UnescapeDataString(root);

            InstallationRoot = Path.GetDirectoryName(rootStr);

            var profilerName = "Profiler.{0}.dll";

            Profilerx86 = Path.Combine(InstallationRoot, "x86", string.Format(profilerName, "x86"));
            Profilerx64 = Path.Combine(InstallationRoot, "x64", string.Format(profilerName, "x64"));

            TestHost = Path.Combine(InstallationRoot, "DebugTools.TestHost.exe");
        }

        public static Process CreateProcess(string processName, params ProfilerEnvFlags[] flags)
        {
            var envVariables = new StringDictionary
            {
                { "COR_ENABLE_PROFILING", "1" },
                { "COR_PROFILER", ProfilerInfo.Guid.ToString("B") },
                { "COR_PROFILER_PATH_32", ProfilerInfo.Profilerx86 },
                { "COR_PROFILER_PATH_64", ProfilerInfo.Profilerx64 },
                { "DEBUGTOOLS_PARENT_PID", Process.GetCurrentProcess().Id.ToString() }
            };

            bool needDebug = false;

            foreach (var flag in flags)
            {
                switch (flag)
                {
                    case ProfilerEnvFlags.WaitForDebugger:
                        needDebug = true;
                        envVariables.Add("DEBUGTOOLS_WAITFORDEBUG", "1");
                        break;

                    default:
                        throw new NotImplementedException($"Don't know how to handle flag '{flag}'.");
                }
            }

            SECURITY_ATTRIBUTES processAttribs = new SECURITY_ATTRIBUTES();
            SECURITY_ATTRIBUTES threadAttribs = new SECURITY_ATTRIBUTES();

            STARTUPINFO si = new STARTUPINFO
            {
                cb = Marshal.SizeOf<STARTUPINFO>(),
                dwFlags = STARTF.STARTF_USESHOWWINDOW,
                wShowWindow = ShowWindow.Minimized
            };

            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();

            //You MUST ensure all global environment variables are defined; otherwise certain programs that assume these variables exist (such as PowerShell) may crash
            foreach (DictionaryEntry environmentVariable in Environment.GetEnvironmentVariables())
                envVariables.Add((string)environmentVariable.Key, (string)environmentVariable.Value);

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

                var process = Process.GetProcessById(pi.dwProcessId);

                if (needDebug)
                    VsDebugger.Attach(process);

                return process;
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
    }
}