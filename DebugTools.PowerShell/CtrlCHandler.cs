using System;

namespace DebugTools.PowerShell
{
    class CtrlCHandler : IDisposable
    {
        private bool disposed;
        private ConsoleCtrlHandlerRoutine routine;

        public CtrlCHandler(ConsoleCtrlHandlerRoutine routine)
        {
            /* When Console.CancelKeyPress is used, the actual event handler is Console.BreakEvent().
             * This dispatches to Console._cancelCallbacks which is a volatile thread-local value. Callbacks appear
             * to be executed in an indeterminate order when Console.CancelKeyPress is used, which is no good because we need
             * to make sure we're executed before PowerShell's default key handler executes. On Windows, PowerShell
             * sets its key handler by calling SetConsoleCtrlHandler directly, and when kernel32!CtrlRoutine
             * is executed, the handler list is executed in reverse order, thereby allowing us to ensure that
             * our handler is invoked first. */
            this.routine = routine;
            NativeMethods.SetConsoleCtrlHandler(routine, true);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            NativeMethods.SetConsoleCtrlHandler(routine, false);

            disposed = true;
        }
    }
}
