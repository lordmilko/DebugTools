using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace DebugTools.Profiler
{
    class LiveProfilerTarget : IProfilerTarget
    {
        public Process Process => config.Process;
        private NamedPipeClientStream pipe;

        public string Name => Process.ProcessName;

        public int? ProcessId => Process.Id;

        public bool IsAlive => Process != null && !Process.HasExited;

        private CancellationTokenSource pipeCTS;

        private LiveProfilerReaderConfig config;

        public LiveProfilerTarget(LiveProfilerReaderConfig config)
        {
            this.config = config;
        }

        public void Start(Action startCallback, CancellationToken cancellationToken)
        {
            pipeCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            ProfilerInfo.CreateProcess(config.ProcessName, p =>
            {
                config.Process = p;

                startCallback();
            }, config.Settings);

            //This will wait for the pipe to be created if it doesn't exist yet
            try
            {
                if (!config.Settings?.Any(s => s == ProfilerSetting.DisablePipe) == true)
                {
                    pipe = new NamedPipeClientStream(".", $"DebugToolsProfilerPipe_{Process.Id}", PipeDirection.Out);

                    pipe.ConnectAsync(config.PipeTimeout, pipeCTS.Token).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException)
            {
                throw GetPipeTimeoutReason(ex);
            }
        }

        public void SetExitHandler(Action<bool> onTargetExit)
        {
            Process.EnableRaisingEvents = true;
            Process.Exited += (s, e) =>
            {
                pipeCTS.Cancel();

                onTargetExit(Process.ExitCode == 0);
            };
        }

        private Exception GetPipeTimeoutReason(Exception ex)
        {
            var eventLog = new EventLog("Application");

            var logs = eventLog.Entries.Cast<EventLogEntry>().Reverse().TakeWhile(e => e.TimeGenerated > DateTime.Now.AddMinutes(-1)).Where(e => e.Source == ".NET Runtime").ToArray();

            var str = "Timed out waiting for named pipe to connect to profiler.";

            if (logs.Length == 0)
            {
                if (IsAlive)
                {
                    if (ProfilerWasInjected(ex))
                        return new ProfilerException($"{str} The runtime did not log an event saying it tried to load the profiler, but the profiler is loaded into the target process.", ex);
                    else
                    {
                        return new ProfilerException($"{str} The profiler was not injected into the target process. It is a managed process. Is the profiler being blocked by your antivirus?", ex);
                    }
                }
                else
                {
                    return new ProfilerException($"{str} The runtime did not log an event saying it tried to load the profiler, and the process has already exited.", ex);
                }
            }

            foreach (var log in logs)
            {
                if (IsLogMatch(log))
                    return new ProfilerException($"{str} The following event was logged: '{log.Message}'", ex);
            }

            return new TimeoutException($"{str} Was the profiler correctly loaded into the target process?", ex);
        }

        private bool ProfilerWasInjected(Exception ex)
        {
            var modules = Process.GetProcessById(Process.Id).Modules;

            var x86 = Path.GetFileName(ProfilerInfo.Profilerx86);
            var x64 = Path.GetFileName(ProfilerInfo.Profilerx64);

            bool hasClr = false;

            foreach (ProcessModule module in modules)
            {
                if (x86.Equals(module.ModuleName, StringComparison.OrdinalIgnoreCase) || x64.Equals(module.ModuleName, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (module.ModuleName?.Equals("clr.dll", StringComparison.OrdinalIgnoreCase) == true || module.ModuleName?.Equals("coreclr.dll") == true)
                    hasClr = true;
            }

            if (!hasClr)
                throw new ProfilerException("Could not find clr.dll or coreclr.dll inside the target process. Are you sure this is a managed process?", ex);

            return false;
        }

        private bool IsLogMatch(EventLogEntry entry)
        {
            var pattern = ".+Process ID \\(decimal\\): (\\d+)\\..+";

            var match = Regex.Match(entry.Message, pattern);

            if (match.Success)
            {
                var pid = Convert.ToInt32(match.Groups[1].Value);

                if (pid == Process.Id)
                    return true;
            }

            return false;
        }

        public void ExecuteCommand(MessageType messageType, object value)
        {
            if (pipe == null)
            {
                var original = Console.ForegroundColor;

                try
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;

                    Console.WriteLine("WARNING: Communication Pipe not active. Only startup profiling will be captured");
                }
                finally
                {
                    Console.ForegroundColor = original;
                }

                return;
            }

            var buffer = new byte[1004];

            var bytes = BitConverter.GetBytes((int)messageType);

            var pos = 0;

            for (; pos < bytes.Length; pos++)
                buffer[pos] = bytes[pos];

            switch (value)
            {
                case long l:
                    bytes = BitConverter.GetBytes(l);
                    break;

                case bool b:
                    bytes = BitConverter.GetBytes(b);
                    break;

                case string s:
                    bytes = Encoding.Unicode.GetBytes(s);
                    break;

                default:
                    throw new NotImplementedException($"Don't know how to handle value of type '{value.GetType().Name}'.");
            }

            if (bytes.Length > buffer.Length - pos)
                throw new InvalidOperationException($"Cannot invoke command {messageType}: value '{value}' was too large.");

            for (var i = 0; i < bytes.Length && pos < buffer.Length; pos++, i++)
                buffer[pos] = bytes[i];

            pipe.Write(buffer, 0, buffer.Length);
        }

        public void Dispose()
        {
            pipe?.Dispose();

            if (IsAlive)
            {
                try
                {
                    Process.Kill();
                }
                finally
                {
                }
            }
        }
    }
}
