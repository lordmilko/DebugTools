using System;
using System.Management.Automation;
using DebugTools.Profiler;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Watch, "DbgProfilerStack")]
    public class WatchDbgProfilerStack : FilterStackFrameCmdlet
    {
        [Parameter(Mandatory = false)]
        public SwitchParameter ExcludeNamespace { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter DisableProgress { get; set; }

        public WatchDbgProfilerStack() : base(true)
        {
        }

        protected override void ProcessRecordEx()
        {
            using (CtrlCHandler())
            {
                ProgressRecord record = null;

                if (!DisableProgress)
                {
                    record = new ProgressRecord(1, "Watch-DbgProfilerStack", "Watching... (Ctrl+C to end)");
                    WriteProgress(record);
                }

                var filter = new FrameFilterer(GetFrameFilterOptions());

                var methodFrameFormatter = new MethodFrameFormatter(ExcludeNamespace);
                var writer = new MethodFrameStringWriter(methodFrameFormatter);

                try
                {
                    foreach (var item in Session.Watch(TokenSource, f => filter.CheckFrameAndClear(f)))
                    {
                        var str = writer.ToString(item);

                        Host.UI.WriteLine(str);
                    }
                }
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    Session.Reset();

                    if (!DisableProgress)
                    {
                        record.RecordType = ProgressRecordType.Completed;
                        WriteProgress(record);
                    }
                }
            }
        }
    }
}
