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

                var threadStacks = session.ThreadCache.Values.ToArray();
                var methods = session.Methods.Values.ToArray();

                if (session.Process.ExitCode != 0)
                    throw new InvalidOperationException($"TestHost exited with exit code {session.Process.ExitCode}");

                var validator = new Validator(threadStacks, methods);

                validate(validator);
            }
        }
    }
}