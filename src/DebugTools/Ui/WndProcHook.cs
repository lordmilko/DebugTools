using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using DebugTools.Profiler;

namespace DebugTools.Ui
{
    public class WndProcHook : IDisposable
    {
        private static IntPtr nativeLib;
        private static IntPtr wndProcHook;
        private static IntPtr wndProcRetHook;

        private IntPtr hhk;
        private IntPtr hhkRet;

        static WndProcHook()
        {
            if (IntPtr.Size == 4)
                nativeLib = NativeMethods.LoadLibrary(ProfilerInfo.Nativex86);
            else
                nativeLib = NativeMethods.LoadLibrary(ProfilerInfo.Nativex64);

            if (nativeLib == IntPtr.Zero)
                throw new InvalidOperationException($"Failed to load DebugTools.Native: {Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()).Message}");

            wndProcHook = NativeMethods.GetProcAddress(nativeLib, "WndProcHook");

            if (wndProcHook == IntPtr.Zero)
                throw new InvalidOperationException($"Failed to get address of WndProcHook: {Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()).Message}");

            wndProcRetHook = NativeMethods.GetProcAddress(nativeLib, "WndProcRetHook");

            if (wndProcRetHook == IntPtr.Zero)
                throw new InvalidOperationException($"Failed to get address of WndProcRetHook: {Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()).Message}");
        }

        public WndProcHook(Process process)
        {
            //WndProcHook should only be accessed inside of HostApp

            var threadId = NativeMethods.GetWindowThreadProcessId(process.MainWindowHandle, out _);

            hhk = NativeMethods.SetWindowsHookEx(
                HookType.WH_CALLWNDPROC,
                Marshal.GetDelegateForFunctionPointer<HOOKPROC>(wndProcHook),
                nativeLib,
                threadId
            );

            if (hhk == IntPtr.Zero)
                throw new InvalidOperationException($"Failed to install WndProcHook: {Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()).Message}");

            hhkRet = NativeMethods.SetWindowsHookEx(
                HookType.WH_CALLWNDPROCRET,
                Marshal.GetDelegateForFunctionPointer<HOOKPROC>(wndProcRetHook),
                nativeLib,
                threadId
            );

            if (hhkRet == IntPtr.Zero)
                throw new InvalidOperationException($"Failed to install WndProcRetHook: {Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()).Message}");
        }

        public void Dispose()
        {
            if (hhk != IntPtr.Zero)
                NativeMethods.UnhookWindowsHookEx(hhk);

            if (hhkRet != IntPtr.Zero)
                NativeMethods.UnhookWindowsHookEx(hhkRet);
        }
    }
}
