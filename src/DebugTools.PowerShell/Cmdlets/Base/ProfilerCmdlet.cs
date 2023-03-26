using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace DebugTools.PowerShell.Cmdlets
{
    public abstract class ProfilerCmdlet : DebugToolsCmdlet
    {
        protected IDisposable CtrlCHandler() => new CtrlCHandler(Console_CancelKeyPress);

        private bool Console_CancelKeyPress(int controltype)
        {
            TokenSource.Cancel();
            return true;
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
