using System;
using System.Runtime.InteropServices;

namespace DebugTools.PowerShell
{
    class SuspendedThread : IDisposable
    {
        private IntPtr hThread;

        public SuspendedThread(int threadId)
        {
            var hThread = NativeMethods.OpenThread(ThreadAccess.SUSPEND_RESUME, false, threadId);

            if (hThread == IntPtr.Zero)
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

            var result = NativeMethods.SuspendThread(hThread);

            if (result == -1)
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

            this.hThread = hThread;
        }

        public void Dispose()
        {
            if (hThread != IntPtr.Zero)
            {
                var result = NativeMethods.ResumeThread(hThread);

                if (result == -1)
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
        }
    }
}
