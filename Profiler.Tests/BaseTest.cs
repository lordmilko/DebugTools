using System;
using System.Linq;
using System.Threading;
using DebugTools.Profiler;

namespace Profiler.Tests
{
    public class BaseTest
    {
        internal void TestInternal(TestType type, string subType, Action<Validator> validate, params ProfilerEnvFlags[] flags)
        {
            using (var session = new ProfilerSession())
            {
                var wait = new AutoResetEvent(false);

                session.TraceEventSession.Source.Completed += () => wait.Set();

                session.Start(CancellationToken.None, $"{ProfilerInfo.TestHost} {type} {subType}", flags, true);

                session.Process.WaitForExit();

                wait.WaitOne();

                session.ThrowOnError();

                var threadStacks = session.ThreadCache.Values.ToArray();
                var methods = session.Methods.Values.ToArray();

                if (session.Process.ExitCode != 0)
                {
                    if (session.Process.ExitCode == 2)
                        throw new InvalidOperationException($"Test '{type}' -> '{subType}' has not been defined in TestHost");

                    throw new InvalidOperationException($"TestHost exited with exit code 0x{session.Process.ExitCode.ToString("X")}");
                }

                var validator = new Validator(threadStacks, methods);

                validate(validator);
            }
        }
    }
}
