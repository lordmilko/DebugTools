using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using DebugTools.PowerShell;

namespace DebugTools
{
    public class VsDebugger
    {
        public static void Attach(Process target)
        {
            using (new MessageFilter())
            {
                if (Debugger.IsAttached)
                {
                    var debuggerDte = GetDTEDebuggingMe();

                    foreach (var process in debuggerDte?.Debugger.LocalProcesses.OfType<EnvDTE80.Process2>())
                    {
                        if (CheckProcessId(target, process))
                        {
                            try
                            {
                                process.Attach2("Native");
                            }
                            catch (Exception ex)
                            {
                                throw new Exception("Failed to attach debugger; make sure mixed mode debugging is disabled", ex);
                            }

                            break;
                        }
                    }
                }
            }
        }

        private static EnvDTE.DTE GetDTEDebuggingMe()
        {
            var currentProcessId = Process.GetCurrentProcess().Id;

            foreach (var process in Process.GetProcessesByName("devenv"))
            {
                var dte = GetDTE(process, false);

                if (dte?.Debugger?.DebuggedProcesses?.OfType<EnvDTE.Process>().Any(p => p.ProcessID == currentProcessId) ?? false)
                {
                    return dte;
                }
            }

            return null;
        }

        private static EnvDTE.DTE GetDTE(Process process, bool ensure)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            do
            {
                var dte = GetDTEInternal(process);

                if (dte != null)
                    return dte;

                if (stopwatch.Elapsed.TotalSeconds > 60)
                    throw new TimeoutException($"Failed to get DTE from process '{process.ProcessName}.exe' (ID: {process.Id}). Confirm whether the target is a Visual Studio process.");
            } while (ensure);

            //If ensure was false, and we didn't get a DTE, our DTE was null
            return null;
        }

        private static EnvDTE.DTE GetDTEInternal(Process process)
        {
            object dte = null;
            var monikers = new IMoniker[1];

            IRunningObjectTable runningObjectTable;
            NativeMethods.GetRunningObjectTable(0, out runningObjectTable);

            IEnumMoniker enumMoniker;
            runningObjectTable.EnumRunning(out enumMoniker);

            IBindCtx bindContext;
            NativeMethods.CreateBindCtx(0, out bindContext);

            do
            {
                monikers[0] = null;

                IntPtr monikersFetched = IntPtr.Zero;
                var hresult = enumMoniker.Next(1, monikers, monikersFetched);

                if (hresult == NativeMethods.S_FALSE)
                {
                    // There's nothing further to enumerate, so fail
                    return null;
                }
                else
                {
                    Marshal.ThrowExceptionForHR(hresult);
                }

                var moniker = monikers[0];

                string fullDisplayName;
                moniker.GetDisplayName(bindContext, null, out fullDisplayName);

                // FullDisplayName will look something like: <ProgID>:<ProcessId>
                var displayNameParts = fullDisplayName.Split(':');

                int displayNameProcessId;
                if (!int.TryParse(displayNameParts.Last(), out displayNameProcessId))
                    continue;

                if (displayNameParts[0].StartsWith("!VisualStudio.DTE", StringComparison.OrdinalIgnoreCase) &&
                    displayNameProcessId == process.Id)
                {
                    //If the specified instance of Visual Studio is already being debugged (i.e. by someone else) we will hang indefinitely
                    //trying to get the DTE; time out instead if we don't hear back within 1 second

                    var cts = new CancellationTokenSource();

                    var task = Task.Run(() =>
                    {
                        runningObjectTable.GetObject(moniker, out dte);
                    }, cts.Token);

                    var isCompleted = task.Wait(TimeSpan.FromSeconds(1));

                    if (!isCompleted)
                        cts.Cancel();
                }
            }
            while (dte == null);

            return (EnvDTE.DTE)dte;
        }

        private static bool CheckProcessId(Process target, EnvDTE80.Process2 process)
        {
            //Doing process.ProcessID doesn't seem to be utilizing our MessageFilter, so we do our own manual retry

            while (true)
            {
                try
                {
                    //If RPC_E_SERVERCALL_RETRYLATER is thrown here, disable breaking on this exception;
                    //we will try again thanks to the message filter
                    if (process.ProcessID == target.Id)
                        return true;

                    return false;
                }
                catch
                {
                    Thread.Sleep(1);
                }
            }
        }
    }
}
