using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Threading;

namespace DebugTools.PowerShell.Cmdlets
{
    public abstract class DebugToolsCmdlet : PSCmdlet
    {
        internal readonly CancellationTokenSource TokenSource = new CancellationTokenSource();

        internal CancellationToken CancellationToken => TokenSource.Token;

        /// <summary>
        /// Interrupts the currently running code to signal the cmdlet has been requested to stop.<para/>
        /// Do not override this method; override <see cref="StopProcessingEx"/> instead.
        /// </summary>
        [ExcludeFromCodeCoverage]
        protected sealed override void StopProcessing()
        {
            StopProcessingEx();

            TokenSource.Cancel();
        }

        /// <summary>
        /// Interrupts the currently running code to signal the cmdlet has been requested to stop.
        /// </summary>
        protected virtual void StopProcessingEx()
        {
        }
    }
}
