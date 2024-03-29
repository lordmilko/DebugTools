﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using ChaosLib.Metadata;
using DebugTools.Profiler;
using ChaosPEFile = ChaosLib.Metadata.PEFile;

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
        public SwitchParameter WinDbg { get; set; }

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
        public SwitchParameter IgnorePointerValue { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter IgnoreDefaultBlacklist { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Synchronous { get; set; }

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

        [Parameter(Mandatory = false)]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            if (PassThru && TraceStart)
                throw new ParameterBindingException($"Cannot specify -{nameof(PassThru)} and -{nameof(TraceStart)} at the same time.");

            if (WinDbg)
            {
                var windbgPath = GetWinDbgAndResolveProcessName();

                ProcessName = $"\"{windbgPath}\" \"{ProcessName}\"";
            }

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

            if (IgnorePointerValue)
                settings.Add(ProfilerSetting.IgnorePointerValue);

            if (IgnoreDefaultBlacklist)
                settings.Add(ProfilerSetting.IgnoreDefaultBlacklist);

            if (Synchronous)
                settings.Add(ProfilerSetting.SynchronousTransfers);

            if (ModuleBlacklist != null)
                settings.Add(ProfilerSetting.ModuleBlacklist(matcher.Execute(ModuleBlacklist)));

            if (ModuleWhitelist != null)
                settings.Add(ProfilerSetting.ModuleWhitelist(matcher.Execute(ModuleWhitelist)));

            var config = GetProfilerConfig(settings.ToArray());

            var session = new ProfilerSession(config);

            session.Start(CancellationToken);

            //We must wait until after the session is started to actually have a PID
            if (session.Type == ProfilerSessionType.XmlFile)
                DebugToolsSessionState.Services.AddSpecial(session);
            else
                DebugToolsSessionState.Services.Add(session.PID.Value, session);

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
            else
            {
                if (PassThru)
                    WriteObject(session);
            }
        }

        private IProfilerReaderConfig GetProfilerConfig(ProfilerSetting[] settings)
        {
            var type = Synchronous ? ProfilerSessionType.MMF : ProfilerSessionType.Normal;

            return new LiveProfilerReaderConfig(type, ProcessName, settings);
        }

        private string GetWinDbgAndResolveProcessName()
        {
            TryResolveProcessName();

            var is32Bit = GetTargetWinDbgArchitecture();

            var windbg = DbgEngProvider.GetWinDbg(is32Bit);

            return windbg;
        }

        private bool GetTargetWinDbgArchitecture()
        {
            bool is32BIt;

            if (File.Exists(ProcessName))
            {
                using (var stream = new FileStream(ProcessName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var file = new ChaosPEFile(stream, false);

                    is32BIt = file.OptionalHeader.Magic == PEMagic.PE32;
                }
            }
            else
            {
                is32BIt = IntPtr.Size == 4;
                WriteWarning($"Could not locate process '{ProcessName}'. Assuming " + (is32BIt ? "32-bit" : "64-bit"));
            }

            return is32BIt;
        }

        private void TryResolveProcessName()
        {
            if (!File.Exists(ProcessName))
            {
                var candidate = ProcessName;

                if (!candidate.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    candidate += ".exe";

                if (!File.Exists(candidate))
                {
                    var result = Environment.GetEnvironmentVariable("PATH")
                        .Split(';')
                        .FirstOrDefault(s => File.Exists(Path.Combine(s, candidate)));

                    if (result != null)
                        ProcessName = Path.Combine(result, candidate);
                }
            }
        }
    }
}
