using System;
using ChaosLib;

namespace DebugTools.PowerShell
{
    class SuspendedThread : IDisposable
    {
        private IntPtr hThread;

        public SuspendedThread(int threadId)
        {
            var hThread = Kernel32.OpenThread(ThreadAccess.SUSPEND_RESUME, false, threadId);

            Kernel32.SuspendThread(hThread);

            this.hThread = hThread;
        }

        public void Dispose()
        {
            if (hThread != IntPtr.Zero)
                Kernel32.ResumeThread(hThread);
        }
    }
}
