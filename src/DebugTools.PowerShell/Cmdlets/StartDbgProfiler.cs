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
        public int ValueDepth { get; set; } = 1;

        [Alias("ChildProcess")]
        [Parameter(Mandatory = false)]
        public string TargetProcess { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter IgnoreDefaultBlacklist { get; set; }

        [Parameter(Mandatory = false)]
        public string[] ModuleWhitelist { get; set; }

        [Parameter(Mandatory = false)]
        public string[] ModuleBlacklist { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter DisablePipe { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter IncludeUnknownUnmanagedTransitions { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Minimized { get; set; }

        protected override void ProcessRecord()
        {
            var session = new ProfilerSession();
            DebugToolsSessionState.ProfilerSessions.Add(session);

            var settings = new List<ProfilerSetting>();

            var matcher = new ModuleWildcardMatcher();

            if (Dbg)
                settings.Add(ProfilerSetting.WaitForDebugger);

            if (Detailed)
                settings.Add(ProfilerSetting.Detailed);

            settings.Add(ProfilerSetting.TraceValueDepth(ValueDepth));

            if (MyInvocation.BoundParameters.ContainsKey(nameof(TargetProcess)))
                settings.Add(ProfilerSetting.TargetProcess(TargetProcess));

            if (TraceStart)
                settings.Add(ProfilerSetting.TraceStart);

            if (DisablePipe)
                settings.Add(ProfilerSetting.DisablePipe);

            if (IncludeUnknownUnmanagedTransitions)
                settings.Add(ProfilerSetting.IncludeUnknownUnmanagedTransitions);

            if (Minimized)
                settings.Add(ProfilerSetting.Minimized);

            if (IgnoreDefaultBlacklist)
                settings.Add(ProfilerSetting.IgnoreDefaultBlacklist);

            if (ModuleBlacklist != null)
                settings.Add(ProfilerSetting.ModuleBlacklist(matcher.Execute(ModuleBlacklist)));

            if (ModuleWhitelist != null)
                settings.Add(ProfilerSetting.ModuleWhitelist(matcher.Execute(ModuleWhitelist)));

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
