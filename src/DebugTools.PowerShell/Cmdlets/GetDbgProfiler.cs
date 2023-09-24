using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using DebugTools.Profiler;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "DbgProfiler")]
    public class GetDbgProfiler : PSCmdlet
    {
        [Alias("ProcessName")]
        [Parameter(Mandatory = false, Position = 0)]
        public string[] Name { get; set; }

        [Alias("PID")]
        [Parameter(Mandatory = false)]
        public int[] Id { get; set; }

        [Parameter(Mandatory = false)]
        public ProfilerSessionType[] Type { get; set; }

        [Parameter(Mandatory = false)]
        public ProfilerSessionStatus[] Status { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Force { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Global { get; set; }

        protected override void ProcessRecord()
        {
            IEnumerable<ProfilerSession> sessions = DebugToolsSessionState.Services.GetSubSessions<ProfilerSession>();

            if (Global)
            {
                var global = DebugToolsSessionState.Services.GetOrCreateSpecial<ProfilerSession>(new CreateSpecialProfilerContext(ProfilerSessionType.Global, false, false));

                if (global != null)
                    WriteObject(global);

                return;
            }

            if (!Force)
                sessions = sessions.Where(i => i.IsAlive);

            if (Id != null && Id.Length > 0)
                sessions = sessions.Where(i => Id.Any(id => id == i.Target.ProcessId));

            if (Name != null && Name.Length > 0)
                sessions = FilterByWildcardArray(Name, sessions, v => v.Target.Name);

            if (Type != null)
                sessions = sessions.Where(s => Type.Any(t => t == s.Type));

            if (Status != null)
                sessions = sessions.Where(s => Status.Any(st => st == s.Status));

            foreach (var instance in sessions)
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
