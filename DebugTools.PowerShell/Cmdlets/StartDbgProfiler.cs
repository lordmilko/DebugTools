using System.Collections.Generic;
using System.Management.Automation;
using DebugTools.Profiler;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Start, "DbgProfiler")]
    public class StartDbgProfiler : ProfilerCmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        public string ProcessName { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Dbg { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter TraceStart { get; set; }

        protected override void ProcessRecord()
        {
            var session = new ProfilerSession();
            ProfilerSessionState.Sessions.Add(session);

            var flags = new List<ProfilerEnvFlags>();

            if (Dbg)
                flags.Add(ProfilerEnvFlags.WaitForDebugger);

            session.Start(CancellationToken, ProcessName, flags.ToArray(), TraceStart);

            if (TraceStart)
            {
                StartCtrlCHandler();

                var record = new ProgressRecord(1, "Start-DbgProfiler", "Tracing... (Ctrl+C to end)");
                WriteProgress(record);

                ThreadStack[] threadStack;

                try
                {
                    threadStack = session.Trace(CancellationToken);
                }
                finally
                {
                    record.RecordType = ProgressRecordType.Completed;
                    WriteProgress(record);
                }

                foreach (var item in threadStack)
                    WriteObject(item.Root);
            }
        }
    }
}