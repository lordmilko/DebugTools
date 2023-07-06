using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using ClrDebug;

namespace DebugTools
{
    public delegate bool ConsoleCtrlHandlerRoutine(int controlType);

    public delegate bool WNDENUMPROC(IntPtr hwnd, IntPtr lParam);

    public static class NativeMethods
    {
        private const string dbghelp = "dbghelp.dll";
        private const string gdi32 = "gdi32.dll";
        private const string kernel32 = "kernel32.dll";
        private const string ntdll = "ntdll.dll";
        private const string ole32 = "ole32.dll";
        private const string shcore = "shcore.dll";
        private const string user32 = "user32.dll";

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
        #region gdi32.dll

        internal const int R2_NOT = 6;

        [DllImport(gdi32, SetLastError = true)]
        public static extern IntPtr CreatePen(PenStyle iStyle, int cWidth, uint color);

        [DllImport(gdi32, SetLastError = true)]
        public static extern bool DeleteObject(IntPtr ho);

        [DllImport(gdi32, SetLastError = true)]
        public static extern IntPtr GetStockObject(StockObject i);

        [DllImport(gdi32, SetLastError = true)]
        public static extern bool Rectangle(IntPtr hdc, int left, int top, int right, int bottom);

        [DllImport(gdi32, SetLastError = true)]
        public static extern bool RestoreDC(IntPtr hdc, int nSavedDC);

        [DllImport(gdi32, SetLastError = true)]
        public static extern int SaveDC(IntPtr hdc);

        [DllImport(gdi32, SetLastError = true)]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

        [DllImport(gdi32, SetLastError = true)]
        public static extern int SetROP2(IntPtr hdc, int rop2);

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
        public static extern bool SetEvent([In] IntPtr hEvent);

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
        #region shcore.dll

        [DllImport(shcore)]
        public static extern HRESULT GetProcessDpiAwareness(IntPtr hprocess, out PROCESS_DPI_AWARENESS value);

        [DllImport(shcore)]
        public static extern HRESULT SetProcessDpiAwareness(PROCESS_DPI_AWARENESS value);

        #endregion
        #region user32.dll

        public static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);
        public static readonly IntPtr HWND_TOP = new IntPtr(0);
        public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        [DllImport(user32, SetLastError = true)]
        public static extern bool EnumWindows([MarshalAs(UnmanagedType.FunctionPtr)] WNDENUMPROC lpEnumFunc, IntPtr lParam);

        [DllImport(user32, SetLastError = true)]
        public static extern bool GetCursorPos(out POINT point);

        [DllImport(user32, SetLastError = true)]
        public static extern int GetDpiForWindow(IntPtr hwnd);

        [DllImport(user32, SetLastError = true)]
        public static extern int GetSystemMetrics(SystemMetric nIndex);

        [DllImport(user32)]
        public static extern IntPtr GetThreadDpiAwarenessContext();

        [DllImport(user32, SetLastError = true)]
        public static extern IntPtr GetWindow(IntPtr hWnd, GetWindowType uCmd);

        [DllImport(user32, SetLastError = true)]
        public static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport(user32, SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport(user32, SetLastError = true)]
        public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport(user32, SetLastError = true)]
        public static extern bool IsWindow(IntPtr hWnd);

        [DllImport(user32, SetLastError = true)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport(user32, SetLastError = true)]
        public static extern bool ReleaseCapture();

        [DllImport(user32, SetLastError = true)]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport(user32, SetLastError = true)]
        public static extern IntPtr SetCapture(IntPtr hWnd);

        [DllImport(user32)]
        public static extern IntPtr SetThreadDpiAwarenessContext(IntPtr dpiContext);

        [DllImport(user32, SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

        [DllImport(user32, SetLastError = true)]
        public static extern IntPtr WindowFromPoint(POINT point);

        #endregion
    }
}
