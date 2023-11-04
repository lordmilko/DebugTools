using System;
using System.Diagnostics;
using System.Threading;
using DebugTools.Memory;

namespace DebugTools.Host
{
    class InjectedHostSession : IDisposable
    {
        private Thread injectorThread;
        private CancellationTokenSource cts = new CancellationTokenSource();

        public Process Process { get; }

        public HostApp App { get; }

        public InjectedHostSession(Process process, bool debug = false)
        {
            Process = process;

            var injector = new DllInjector(process);

            Exception exception = null;

            injectorThread = new Thread(() =>
            {
                try
                {
                    injector.Inject(typeof(HostApp).GetMethod("MainNative"), debug ? "-debug" : string.Empty);
                }
                catch (Exception ex)
                {
                    exception = ex;
                    cts.Cancel();
                }
            });
            injectorThread.Start();

            try
            {
                App = HostProvider.CreateApp(process, debug, token: cts.Token);
            }
            catch (OperationCanceledException)
            {
                if (exception != null)
                    throw exception;

                throw;
            }
        }

        public void Dispose()
        {
            if (App != null)
                App.Exit();

            //Wait for the injector thread to exit
            injectorThread.Join();
        }
    }
}
