using System;
using System.Collections.Generic;

namespace DebugTools.Ui
{
    class MainWindowEnumerator
    {
        private List<IntPtr> candidateWindows = new List<IntPtr>();
        private IntPtr match;

        public bool TryGetMainWindows(int pid, out IntPtr hWnd)
        {
            WNDENUMPROC callback = EnumWindowsCallback;

            User32.EnumWindows(callback, new IntPtr(pid));

            GC.KeepAlive(callback);

            if (match == IntPtr.Zero)
            {
                hWnd = default;
                return false;
            }

            hWnd = match;
            return true;
        }

        private bool EnumWindowsCallback(IntPtr hwnd, IntPtr lParam)
        {
            User32.GetWindowThreadProcessId(hwnd, out var pid);

            if ((int)lParam == pid)
            {
                var owner = User32.GetWindow(hwnd, GetWindowType.GW_OWNER);
                var isVisible = User32.IsWindowVisible(hwnd);

                if (owner != IntPtr.Zero && isVisible)
                {
                    match = hwnd;
                    return false;
                }
            }

            return true;
        }
    }
}
