using System;
using System.Diagnostics;
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

                    bool lastWasCtrl = false;

                    while (true)
                    {
                        var key = this.Host.UI.RawUI.ReadKey(
                            ReadKeyOptions.AllowCtrlC | ReadKeyOptions.IncludeKeyUp | ReadKeyOptions.NoEcho);

                        Debug.WriteLine(key);

                        //Our makeshift Ctrl+C handler is a bit crap sometimes and detects a perfectly coherent Ctrl+C
                        //as a CTRL followed by a C; as such, track whether it most likely looks like the user attempted to do a Ctrl+C
                        if (key.VirtualKeyCode == 17)
                            lastWasCtrl = true;
                        else if (key.VirtualKeyCode == 67 && lastWasCtrl)
                            break;
                        else
                            lastWasCtrl = false;

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