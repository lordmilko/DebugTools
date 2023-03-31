using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DebugTools.Profiler;

namespace Profiler.Tests
{
    public class BaseTest
    {
        protected RootFrame MakeRoot(IMethodFrame child, int threadId) =>
            MakeRoot(new[] {child}, threadId);

        protected RootFrame MakeRoot(params IMethodFrame[] children) =>
            MakeRoot(children, 1000);

        protected RootFrame MakeRoot(IMethodFrame[] children, int threadId)
        {
            var newFrame = new RootFrame
            {
                ThreadId = threadId,
            };

            if (children != null)
            {
                newFrame.Children.AddRange(children);

                foreach (var child in children)
                    child.Parent = newFrame;
            }

            int sequence = 0;

            SetSequence(children, ref sequence);

            return newFrame;
        }

        private void SetSequence(IEnumerable<IMethodFrame> children, ref int sequence)
        {
            foreach (var child in children)
            {
                sequence++;

                if (child is MockUnmanagedTransitionFrame u)
                    u.Sequence = sequence;
                else
                    ((MockMethodFrameDetailed) child).Sequence = sequence;

                SetSequence(child.Children, ref sequence);
            }
        }

        protected IMethodFrameDetailed MakeFrame(string methodName, object parameter, params IMethodFrame[] children) =>
            MakeFrame(typeof(Methods).GetMethod(methodName), parameter, children);

        protected IMethodFrameDetailed MakeFrame(System.Reflection.MethodInfo method, object parameter, params IMethodFrame[] children)
        {
            byte[] enterBytes = null;
            byte[] exitBytes = null;

            if (parameter is IMockValue v)
            {
                parameter = v.OuterValue;
                enterBytes = v.RawBytes;
                exitBytes = v.RawBytes;
            }

            var parameters = new List<object>();

            if (parameter != null)
                parameters.Add(parameter);

            var newFrame = new MockMethodFrameDetailed(
                new MockMethodInfoDetailed(method),
                parameters,
                VoidValue.Instance
            )
            {
                EnterValue = enterBytes,
                ExitValue = exitBytes
            };

            if (children != null)
            {
                newFrame.Children.AddRange(children);

                foreach (var child in children)
                    child.Parent = newFrame;
            }

            return newFrame;
        }

        protected IUnmanagedTransitionFrame MakeUnmanagedFrame(string methodName, params IMethodFrame[] children)
        {
            var newFrame = new MockUnmanagedTransitionFrame(
                new MockMethodInfo(typeof(Methods).GetMethod(methodName)),
                FrameKind.M2U
            );

            if (children != null)
            {
                newFrame.Children.AddRange(children);

                foreach (var child in children)
                    child.Parent = newFrame;
            }

            return newFrame;
        }

        protected IFrame[] Flatten(IFrame frame, bool includeRoot = false) =>
            FlattenInternal(frame, includeRoot).ToArray();

        private IEnumerable<IFrame> FlattenInternal(IFrame frame, bool includeRoot)
        {
            if (!(frame is IRootFrame) || includeRoot)
                yield return frame;

            foreach (var child in frame.Children)
            {
                foreach (var item in FlattenInternal(child, includeRoot))
                    yield return item;
            }
        }

        internal void TestInternal(TestType type, string subType, Action<Validator> validate, params ProfilerSetting[] settings)
        {
            var settingsList = settings.ToList();
            settingsList.Add(ProfilerSetting.TraceStart);

            using (var session = new ProfilerSession())
            {
                var wait = new AutoResetEvent(false);

                session.TraceEventSession.Source.Completed += () => wait.Set();

                session.Start(CancellationToken.None, $"{ProfilerInfo.TestHost} {type} {subType}", settingsList.ToArray());

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
