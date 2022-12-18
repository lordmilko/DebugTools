using System;
using System.Management.Automation;
using System.Threading;
using DebugTools.Profiler;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsDiagnostic.Trace, "DbgProfilerStack")]
    public class TraceDbgProfilerStack : ProfilerSessionCmdlet
    {
        private CancellationTokenSource traceCTS = new CancellationTokenSource();

        protected override void ProcessRecordEx()
        {
            StartCtrlCHandler();

            var record = new ProgressRecord(1, "Trace-DbgProfilerStack", "Tracing... (Ctrl+C to end)");
            WriteProgress(record);

            ThreadStack[] threadStack;

            try
            {
                threadStack = Session.Trace(CancellationToken);
            }
            finally
            {
                record.RecordType = ProgressRecordType.Completed;
                WriteProgress(record);
            }

            foreach (var item in threadStack)
                WriteObject(item.Root);
        }

        private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            traceCTS.Cancel();
        }
    }
}