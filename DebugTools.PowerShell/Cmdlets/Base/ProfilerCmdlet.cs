using System;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Threading;

namespace DebugTools.PowerShell.Cmdlets
{
    public abstract class ProfilerCmdlet : PSCmdlet
    {
        internal readonly CancellationTokenSource TokenSource = new CancellationTokenSource();

        internal CancellationToken CancellationToken => TokenSource.Token;

        /// <summary>
        /// Interrupts the currently running code to signal the cmdlet has been requested to stop.<para/>
        /// Do not override this method; override <see cref="StopProcessingEx"/> instead.
        /// </summary>
        [ExcludeFromCodeCoverage]
        protected override void StopProcessing()
        {
            StopProcessingEx();

            TokenSource.Cancel();
        }

        protected IDisposable CtrlCHandler() => new CtrlCHandler(Console_CancelKeyPress);

        private bool Console_CancelKeyPress(int controltype)
        {
            TokenSource.Cancel();
            return true;
        }

        /// <summary>
        /// Interrupts the currently running code to signal the cmdlet has been requested to stop.
        /// </summary>
        protected virtual void StopProcessingEx()
        {
        }
    }
}
