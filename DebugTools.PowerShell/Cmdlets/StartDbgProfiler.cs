using System;
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
        public SwitchParameter Detailed { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter TraceStart { get; set; }

        [Parameter(Mandatory = false)]
        public int ValueDepth { get; set; }

        [Alias("ChildProcess")]
        [Parameter(Mandatory = false)]
        public string TargetProcess { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter DisablePipe { get; set; }

        protected override void ProcessRecord()
        {
            var session = new ProfilerSession();
            ProfilerSessionState.Sessions.Add(session);

            var settings = new List<ProfilerSetting>();

            if (Dbg)
                settings.Add(ProfilerSetting.WaitForDebugger);

            if (Detailed)
                settings.Add(ProfilerSetting.Detailed);

            if (MyInvocation.BoundParameters.ContainsKey(nameof(ValueDepth)))
                settings.Add(new ProfilerSetting(ProfilerEnvFlags.TraceValueDepth, ValueDepth));

            if (MyInvocation.BoundParameters.ContainsKey(nameof(TargetProcess)))
                settings.Add(new ProfilerSetting(ProfilerEnvFlags.TargetProcess, TargetProcess));

            if (TraceStart)
                settings.Add(ProfilerSetting.TraceStart);

            if (DisablePipe)
                settings.Add(ProfilerSetting.DisablePipe);

            session.Start(CancellationToken, ProcessName, settings.ToArray());

            if (TraceStart)
            {
                using (CtrlCHandler())
                {
                    var record = new ProgressRecord(1, "Start-DbgProfiler", "Tracing... (Ctrl+C to end)");
                    WriteProgress(record);

                    ThreadStack[] threadStack;

                    try
                    {
                        threadStack = session.Trace(TokenSource);
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
}
