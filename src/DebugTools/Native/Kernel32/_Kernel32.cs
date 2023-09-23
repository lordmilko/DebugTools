using System;
using System.Runtime.InteropServices;
using ClrDebug;

namespace DebugTools
{
    public delegate bool ConsoleCtrlHandlerRoutine(int controlType);

    static class Kernel32
    {
        private const string kernel32 = "kernel32.dll";

        internal const int S_FALSE = 1;

        [DllImport(kernel32, SetLastError = true)]
        public static extern bool CloseHandle(IntPtr handle);

        [DllImport(kernel32, SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern bool CreateProcessA(
            string lpApplicationName,
            string lpCommandLine,
            ref SECURITY_ATTRIBUTES lpProcessAttributes,
            ref SECURITY_ATTRIBUTES lpThreadAttributes,
            bool bInheritHandles,
            CreateProcessFlags dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            [In] ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport(kernel32, SetLastError = true)]
        internal static extern bool GetThreadContext(IntPtr hThread, IntPtr lpContext);

        [DllImport(kernel32, SetLastError = true)]
        public static extern bool IsWow64Process(
            [In] IntPtr hProcess,
            [Out, MarshalAs(UnmanagedType.Bool)] out bool Wow64Process);

        [DllImport("kernel32.dll", EntryPoint = "LoadLibraryW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr LoadLibrary(string lpLibFileName);

        [DllImport(kernel32, SetLastError = true)]
        public static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, int dwThreadId);

        #region ReadProcessMemory

        [DllImport(kernel32, SetLastError = true)]
        public static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            [Out] IntPtr lpBuffer,
            int dwSize,
            out int lpNumberOfBytesRead);

        public static byte[] ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            int dwSize)
        {
            byte[] buffer;
            TryReadProcessMemory(hProcess, lpBaseAddress, dwSize, out buffer).ThrowOnNotOK();
            return buffer;
        }

        public static HRESULT TryReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            int dwSize,
            out byte[] buffer)
        {
            var buff = Marshal.AllocHGlobal(dwSize);

            try
            {
                var result = Kernel32.ReadProcessMemory(
                    hProcess,
                    lpBaseAddress,
                    buff,
                    dwSize,
                    out var lpNumberOfBytesRead
                );

                if (!result)
                {
                    buffer = null;
                    return (HRESULT)Marshal.GetHRForLastWin32Error();
                }

                buffer = new byte[lpNumberOfBytesRead];
                Marshal.Copy(buff, buffer, 0, lpNumberOfBytesRead);
                return HRESULT.S_OK;
            }
            finally
            {
                Marshal.FreeHGlobal(buff);
            }
        }

        #endregion

        [DllImport(kernel32, SetLastError = true)]
        internal static extern int ResumeThread(IntPtr hThread);

        [DllImport(kernel32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool SetDllDirectory(string lpPathName);

        [DllImport(kernel32, SetLastError = true)]
        public static extern bool SetEvent([In] IntPtr hEvent);

        [DllImport(kernel32, SetLastError = true)]
        public static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandlerRoutine HandlerRoutine, bool Add);

        [DllImport(kernel32, SetLastError = true)]
        internal static extern int SuspendThread(IntPtr hThread);

        [DllImport(kernel32, EntryPoint = "RtlZeroMemory", SetLastError = false)]
        internal static extern void ZeroMemory(IntPtr dest, int size);
    }
}
