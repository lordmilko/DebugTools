using System;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Threading;
using System.Threading.Tasks;

namespace DebugTools.PowerShell.Cmdlets
{
    public abstract class ProfilerCmdlet : PSCmdlet
    {
        private readonly CancellationTokenSource TokenSource = new CancellationTokenSource();

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

        protected void StartCtrlCHandler()
        {
            Task.Run(() =>
            {
                var original = Console.TreatControlCAsInput;

                try
                {
                    Console.TreatControlCAsInput = true;

                    while (true)
                    {
                        var key = this.Host.UI.RawUI.ReadKey(
                            ReadKeyOptions.AllowCtrlC | ReadKeyOptions.IncludeKeyUp | ReadKeyOptions.NoEcho);

                        if ((key.ControlKeyState == ControlKeyStates.LeftCtrlPressed ||
                             key.ControlKeyState == ControlKeyStates.RightCtrlPressed) && key.Character == 3)
                            break;
                    }

                    TokenSource.Cancel();
                }
                finally
                {
                    Console.TreatControlCAsInput = original;
                }
            });
        }

        /// <summary>
        /// Interrupts the currently running code to signal the cmdlet has been requested to stop.
        /// </summary>
        protected virtual void StopProcessingEx()
        {
        }
    }
}