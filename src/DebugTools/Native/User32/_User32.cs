using System;
using System.Runtime.InteropServices;

namespace DebugTools
{
    public delegate IntPtr HOOKPROC(int code, IntPtr wParam, IntPtr lParam);

    public delegate bool WNDENUMPROC(IntPtr hwnd, IntPtr lParam);

    static class User32
    {
        private const string user32 = "user32.dll";

        public static readonly IntPtr HWND_TOP = new IntPtr(0);
        public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        public static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);

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

        [DllImport(user32, SetLastError = true)]
        public static extern IntPtr SendMessageW(
            IntPtr hWnd,
            WM Msg,
            IntPtr wParam,
            IntPtr lParam);

        [DllImport(user32)]
        public static extern IntPtr SetThreadDpiAwarenessContext(IntPtr dpiContext);

        [DllImport(user32, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(
            HookType idHook,
            [MarshalAs(UnmanagedType.FunctionPtr)] HOOKPROC lpfn,
            IntPtr hmod,
            int dwThreadId);

        [DllImport(user32, SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

        [DllImport(user32, SetLastError = true)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport(user32, SetLastError = true)]
        public static extern IntPtr WindowFromPoint(POINT point);
    }
}
