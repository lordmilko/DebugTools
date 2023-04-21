using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using DebugTools.Tracing;
using Microsoft.Diagnostics.Tracing.Session;

namespace DebugTools.Profiler
{
    class LiveEtwProfilerReader : EtwProfilerReader
    {
        private static int maxId;
        private const string SessionPrefix = "DebugTools_Profiler_";

        private static object lockObj = new object();

        public override event Action Completed
        {
            add => TraceEventSession.Source.Completed += value;
            remove => TraceEventSession.Source.Completed -= value;
        }

        public TraceEventSession TraceEventSession { get; private set; }

        public new LiveProfilerReaderConfig Config => (LiveProfilerReaderConfig) base.Config;

        public LiveEtwProfilerReader(LiveProfilerReaderConfig config) : base(config)
        {
            var sessionName = GetNextSessionName();

            TryCloseSession(sessionName);

            //Events MUST be received in the order they are dispatched, otherwise our shadow stack will completely be broken. It's unclear whether
            //the number of ELT events are generate exceeds the threshold specified by NoPerProcessorBuffering (it probably does) but the reliability
            //of receiving ETW events in the correct order is most important. If events get dropped because of this setting, we will end up throwing
            var sessionOptions = TraceEventSessionOptions.Create | TraceEventSessionOptions.NoPerProcessorBuffering;

            if (config.FileName == null)
                TraceEventSession = new TraceEventSession(sessionName, sessionOptions);
            else
                TraceEventSession = new TraceEventSession(sessionName, config.FileName, sessionOptions);

            Parser = new ProfilerTraceEventParser(TraceEventSession.Source);
        }

        private string GetNextSessionName()
        {
            return $"{SessionPrefix}{Process.GetCurrentProcess().Id}_" + ++maxId;
        }

        private void TryCloseSession(string sessionName)
        {
            var sessions = TraceEventSession.GetActiveSessionNames().Where(n => n.StartsWith(SessionPrefix)).ToArray();

            var processes = Process.GetProcesses();

            foreach (var session in sessions)
            {
                var match = Regex.Match(session, $"{SessionPrefix}(.+?)_.+?");

                if (match.Success && int.TryParse(match.Groups[1].Value, out var pid))
                {
                    if (processes.All(p => p.Id != pid))
                        TraceEventSession.GetActiveSession(session)?.Stop();
                }
            }
        }

        public override void Initialize()
        {
            //If we're only targeting a specific child process, we can't filter based on the process we just created
            TraceEventProviderOptions options = IsTargetingChildProcess(Config.Settings, Config.ProcessName)
                ? null
                : new TraceEventProviderOptions { ProcessIDFilter = new[] { Config.Process.Id } };

            EnableProviderSafe(() => TraceEventSession.EnableProvider(ProfilerTraceEventParser.ProviderGuid, options: options));
        }

        public override void InitializeGlobal()
        {
            EnableProviderSafe(() => TraceEventSession.EnableProvider(ProfilerTraceEventParser.ProviderGuid));
        }

        private bool IsTargetingChildProcess(ProfilerSetting[] settings, string processName)
        {
            if (settings == null)
                return false;

            var setting = settings.FirstOrDefault(s => s.Flag == ProfilerEnvFlags.TargetProcess);

            if (setting == null)
                return false;

            //processName could be a process and some command line args, however in the simple case it's just a process name.
            //Specifying this property is useful when you want to profile Visual Studio but not any of the processes it spawns
            if (processName.EndsWith(setting.StringValue, StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private void EnableProviderSafe(Action action)
        {
            /* In our unit tests, we've seen an issue wherein EnableProvider might randomly fail with "The instance name passed was not recognized as valid by a WMI data provider."
             * Our profiler is not a globally registered provider. It's not that you have to register the profiler first,
             * you can call EnableProvider against a random GUID and it will still succeed. We can stress test running tests in a loop
             * on Appveyor and eventually it will fail. Not sure if it occurs when not executing tests simultaneously; might not have tested one
             * test at a time long enough. In any case, it does seem like not attempting to allow multiple people to call EnableProvider at
             * once does increase the reliability of things
             * */
            lock (lockObj)
            {
                action();
            }
        }

        public override void Execute()
        {
            TraceEventSession.Source.Process();
        }

        public override void Stop()
        {
            //Calling Dispose() guarantees an immediate stop
            TraceEventSession.Dispose();
        }

        public override IProfilerTarget CreateTarget() => new LiveProfilerTarget(Config);

        public override void Dispose()
        {
            TraceEventSession?.Dispose();

            base.Dispose();
        }
    }
}
