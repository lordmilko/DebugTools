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

            NativeMethods.EnumWindows(callback, new IntPtr(pid));

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
            NativeMethods.GetWindowThreadProcessId(hwnd, out var pid);

            if ((int)lParam == pid)
            {
                var owner = NativeMethods.GetWindow(hwnd, GetWindowType.GW_OWNER);
                var isVisible = NativeMethods.IsWindowVisible(hwnd);

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
