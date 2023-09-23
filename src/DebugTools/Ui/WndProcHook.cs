using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ChaosLib;
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
                nativeLib = Kernel32.LoadLibrary(ProfilerInfo.Nativex86);
            else
                nativeLib = Kernel32.LoadLibrary(ProfilerInfo.Nativex64);

            wndProcHook = Kernel32.GetProcAddress(nativeLib, "WndProcHook");
            wndProcRetHook = Kernel32.GetProcAddress(nativeLib, "WndProcRetHook");
        }

        public WndProcHook(Process process)
        {
            //WndProcHook should only be accessed inside of HostApp

            var threadId = User32.GetWindowThreadProcessId(process.MainWindowHandle, out _);

            hhk = User32.SetWindowsHookEx(
                HookType.WH_CALLWNDPROC,
                Marshal.GetDelegateForFunctionPointer<HOOKPROC>(wndProcHook),
                nativeLib,
                threadId
            );

            if (hhk == IntPtr.Zero)
                throw new InvalidOperationException($"Failed to install WndProcHook: {Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()).Message}");

            hhkRet = User32.SetWindowsHookEx(
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
                User32.UnhookWindowsHookEx(hhk);

            if (hhkRet != IntPtr.Zero)
                User32.UnhookWindowsHookEx(hhkRet);
        }
    }
}
