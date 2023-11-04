using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Threading;

namespace DebugTools.PowerShell.Cmdlets
{
    public abstract class DebugToolsCmdlet : PSCmdlet
    {
        private static readonly PropertyInfo wildcardPatternConvertedToRegex = typeof(WildcardPattern).GetInternalPropertyInfo("PatternConvertedToRegex");

        internal readonly CancellationTokenSource TokenSource = new CancellationTokenSource();

        internal CancellationToken CancellationToken => TokenSource.Token;

        protected string GetWildcardRegex(string pattern)
        {
            if (pattern == null)
                return null;

            var wildcard = new WildcardPattern(pattern, WildcardOptions.IgnoreCase);

            return (string) wildcardPatternConvertedToRegex.GetValue(wildcard);
        }

        protected string[] GetWildcardRegex(string[] pattern) => pattern?.Select(GetWildcardRegex).ToArray();

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
