using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using DebugTools.SOS;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "SOSProcess")]
    public class GetSOSProcess : PSCmdlet
    {
        [Parameter(Mandatory = false, Position = 0)]
        public string[] Name { get; set; }

        [Alias("PID")]
        [Parameter(Mandatory = false)]
        public int[] Id { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Force { get; set; }

        protected override void ProcessRecord()
        {
            IEnumerable<LocalSOSProcess> processes = DebugToolsSessionState.Services.GetServices<LocalSOSProcess>();

            if (!Force)
                processes = processes.Where(i => !i.Process.HasExited);

            if (Id != null && Id.Length > 0)
                processes = processes.Where(i => Id.Any(id => id == i.Process.Id));

            if (Name != null && Name.Length > 0)
                processes = FilterByWildcardArray(Name, processes, v => v.Process.ProcessName);

            foreach (var instance in processes)
                WriteObject(instance);
        }

        internal static IEnumerable<T> FilterByWildcardArray<T>(string[] arr, IEnumerable<T> records, params Func<T, string>[] getProperty)
        {
            if (arr != null)
            {
                records = records.Where(
                    record => arr
                        .Select(a => new WildcardPattern(a, WildcardOptions.IgnoreCase))
                        .Any(filter => getProperty.Any(p => filter.IsMatch(p(record))))
                );
            }

            return records;
        }
    }
}
