using System;
using System.Runtime.InteropServices;

namespace DebugTools.PowerShell
{
    class SuspendedThread : IDisposable
    {
        private IntPtr hThread;

        public SuspendedThread(int threadId)
        {
            var hThread = Kernel32.OpenThread(ThreadAccess.SUSPEND_RESUME, false, threadId);

            if (hThread == IntPtr.Zero)
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

            var result = Kernel32.SuspendThread(hThread);

            if (result == -1)
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

            this.hThread = hThread;
        }

        public void Dispose()
        {
            if (hThread != IntPtr.Zero)
            {
                var result = Kernel32.ResumeThread(hThread);

                if (result == -1)
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
        }
    }
}
