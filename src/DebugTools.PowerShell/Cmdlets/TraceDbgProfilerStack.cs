using System.Management.Automation;
using DebugTools.Profiler;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsDiagnostic.Trace, "DbgProfilerStack")]
    public class TraceDbgProfilerStack : ProfilerSessionCmdlet
    {
        public TraceDbgProfilerStack() : base(true)
        {
        }

        protected override void ProcessRecordEx()
        {
            using (CtrlCHandler())
            {
                var record = new ProgressRecord(1, "Trace-DbgProfilerStack", "Tracing... (Ctrl+C to end)");
                WriteProgress(record);

                ThreadStack[] threadStack;

                try
                {
                    threadStack = Session.Trace(TokenSource);
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
