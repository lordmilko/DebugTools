using System;
using System.Diagnostics;

namespace DebugTools.Ui
{
    class UiMessageSession : IDisposable
    {
        private WndProcHook hook;
        private WndProcMonitor monitor;

        public UiMessageSession(Process process)
        {
            monitor = new WndProcMonitor(process);
            hook = new WndProcHook(process);

            monitor.Start();
        }

        public bool TryReadMessage(out WindowMessage message) =>
            monitor.TryReadMessage(out message);

        public void Dispose()
        {
            monitor?.Stop();
            monitor?.Dispose();
            hook?.Dispose();
        }
    }
}
