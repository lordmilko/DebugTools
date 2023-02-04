using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace DebugTools
{
    public delegate bool ConsoleCtrlHandlerRoutine(int controlType);

    public static class NativeMethods
    {
        private const string dbghelp = "dbghelp.dll";
        private const string kernel32 = "kernel32.dll";
        private const string ntdll = "ntdll.dll";
        private const string ole32 = "ole32.dll";

        internal const int S_FALSE = 1;

        #region dbghelp.dll

        [DllImport(dbghelp)]
        internal static extern bool SymCleanup(
            [In] IntPtr hProcess);

        [DllImport(dbghelp, EntryPoint = "SymInitializeW", SetLastError = true)]
        internal static extern bool SymInitialize(
            [In] IntPtr hProcess,
            [In] string UserSearchPath,
            [In] bool fInvadeProcess);

        [DllImport(dbghelp, SetLastError = true)]
        internal static extern bool SymFromAddr(
            [In] IntPtr hProcess,
            [In] ulong address,
            [Out] out long displacement,
            [Out] IntPtr pSymbolInfo);

        [DllImport(dbghelp, SetLastError = true)]
        public static extern bool SymGetTypeInfo(
            [In] IntPtr hProcess,
            [In] ulong ModBase,
            [In] int TypeId,
            [In] IMAGEHLP_SYMBOL_TYPE_INFO GetType,
            [Out] out IntPtr pInfo);

        [DllImport(dbghelp, SetLastError = true)]
        internal static extern ulong SymLoadModuleExW(
            [In] IntPtr hProcess,
            [In, Optional] IntPtr hFile,
            [In, Optional, MarshalAs(UnmanagedType.LPWStr)] string ImageName,
            [In, Optional, MarshalAs(UnmanagedType.LPWStr)] string ModuleName,
            [In, Optional] ulong BaseOfDll,
            [In, Optional] int DllSize,
            [In, Optional] IntPtr Data,
            [In, Optional] int Flags
        );

        #endregion
        #region kernel32.dll

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

        [DllImport(kernel32, SetLastError = true)]
        internal static extern bool GetThreadContext(IntPtr hThread, IntPtr lpContext);

        [DllImport(kernel32, SetLastError = true)]
        public static extern bool IsWow64Process(
            [In] IntPtr hProcess,
            [Out, MarshalAs(UnmanagedType.Bool)] out bool Wow64Process);

        [DllImport(kernel32, SetLastError = true)]
        public static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, int dwThreadId);

        [DllImport(kernel32, SetLastError = true)]
        public static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            [Out] IntPtr lpBuffer,
            int dwSize,
            out int lpNumberOfBytesRead);

        [DllImport(kernel32, SetLastError = true)]
        internal static extern int ResumeThread(IntPtr hThread);

        [DllImport(kernel32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool SetDllDirectory(string lpPathName);

        [DllImport(kernel32, SetLastError = true)]
        public static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandlerRoutine HandlerRoutine, bool Add);

        [DllImport(kernel32, SetLastError = true)]
        internal static extern int SuspendThread(IntPtr hThread);

        [DllImport(kernel32, EntryPoint = "RtlZeroMemory", SetLastError = false)]
        internal static extern void ZeroMemory(IntPtr dest, int size);

        #endregion
        #region ntdll.dll

        [DllImport(ntdll)]
        public static extern unsafe RTL_DEBUG_INFORMATION* RtlCreateQueryDebugBuffer(
            [In] int MaximumCommit,
            [In] bool UseEventPair);

        [DllImport(ntdll)]
        public static extern unsafe int RtlDestroyQueryDebugBuffer(
            [In] RTL_DEBUG_INFORMATION* Buffer);

        [DllImport(ntdll)]
        public static extern unsafe int RtlQueryProcessDebugInformation(
            [In] int UniqueProcessId,
            [In] RtlQueryProcessFlag DebugInfoClassMask,
            [In, Out] RTL_DEBUG_INFORMATION* DebugBuffer);

        #endregion
        #region ole32.dll

        [DllImport(ole32, SetLastError = true)]
        public static extern int CoRegisterMessageFilter(IMessageFilter messageFilter, out IMessageFilter oldMessageFilter);

        [DllImport(ole32, PreserveSig = false)]
        public static extern void CreateBindCtx(int reserved, [MarshalAs(UnmanagedType.Interface)] out IBindCtx bindContext);

        [DllImport(ole32, PreserveSig = false)]
        public static extern void GetRunningObjectTable(int reserved, [MarshalAs(UnmanagedType.Interface)] out IRunningObjectTable runningObjectTable);

        #endregion
    }
}
