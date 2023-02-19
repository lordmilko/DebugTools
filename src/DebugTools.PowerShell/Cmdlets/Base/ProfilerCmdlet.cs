using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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

        internal static IEnumerable<T> FilterByWildcardArray<T>(string[] arr, IEnumerable<T> records, params Func<T, string>[] getProperty)
        {
            if (arr != null)
            {
                records = records.Where(
                    record => arr
                        .Select(a => new WildcardPattern(a, WildcardOptions.IgnoreCase))
                        .Any(filter => getProperty.Any(p => filter.IsMatch(p(record)))
                        )
                );
            }

            return records;
        }
    }
}
