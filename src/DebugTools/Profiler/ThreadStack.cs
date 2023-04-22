using System;
using System.Collections.Generic;
using ClrDebug;
using DebugTools.Tracing;

namespace DebugTools.Profiler
{
    public class ThreadStack
    {
        internal int ThreadId { get; }

        public IFrame Current { get; set; }

        public IRootFrame Root => Current?.GetRoot();

        public Dictionary<long, ExceptionInfo> Exceptions { get; } = new Dictionary<long, ExceptionInfo>();

        private bool includeUnknownTransitions;
        private long lastSequence;

        public ThreadStack(bool includeUnknownTransitions, int threadId)
        {
            this.includeUnknownTransitions = includeUnknownTransitions;
            ThreadId = threadId;
        }

        #region CallArgs

        public IFrame Enter(CallArgs args, IMethodInfo method)
        {
            ValidateSequence(args);

            var newFrame = new MethodFrame(method, args.Sequence);

            if (Current == null)
                Current = new RootFrame { ThreadId = args.ThreadID };

            newFrame.Parent = Current;
            Current.Children.Add(newFrame);

            Current = newFrame;
            return newFrame;
        }

        public void Leave(CallArgs args, IMethodInfo method)
        {
            ValidateEnd(args, method);

            EndCallInternal();
        }

        public void Tailcall(CallArgs args, IMethodInfo method)
        {
            ValidateEnd(args, method);

            //As per Dave Broman's blog, the correct way to handle a tailcall is exactly the same way you would handle a leave call
            //https://web.archive.org/web/20190110160434/https://blogs.msdn.microsoft.com/davbr/2007/06/20/enter-leave-tailcall-hooks-part-2-tall-tales-of-tail-calls/
            EndCallInternal();
        }

        #endregion
        #region CallArgsDetailed

        public IFrame EnterDetailed(CallDetailedArgs args, IMethodInfo method)
        {
            ValidateSequence(args);

            var newFrame = new MethodFrameDetailed(method, args);

            if (Current == null)
                Current = new RootFrame { ThreadId = args.ThreadID };

            newFrame.Parent = Current;
            Current.Children.Add(newFrame);

            Current = newFrame;
            return newFrame;
        }

        public void LeaveDetailed(CallDetailedArgs args, IMethodInfo method)
        {
            ValidateEnd(args, method);

            /* We validate that we don't skip a sequence, however if we started tracing
             * after the process was already started, the first frame was see could be halfway
             * up a stack frame, which means we could receive a large number of Leave calls
             * against Enter calls we never saw (Current will remain as RootFrame for each
             * of these unknown Leave calls) */
            if (Current is MethodFrameDetailed d)
            {
                d.ExitValue = args.HRESULT == HRESULT.S_OK ? args.Value : null;
            }

            EndCallInternal();
        }

        public void TailcallDetailed(CallDetailedArgs args, IMethodInfo method)
        {
            ValidateEnd(args, method);

            EndCallInternal();
        }

        #endregion
        #region UnmanagedTransition

        internal IFrame EnterUnmanagedTransition(UnmanagedTransitionArgs args, IMethodInfoInternal method, FrameKind kind)
        {
            ValidateSequence(args);

            if (method.WasUnknown && !includeUnknownTransitions)
                return null;

            var newFrame = new UnmanagedTransitionFrame(method, args.Sequence, kind);

            if (Current == null)
                Current = new RootFrame { ThreadId = args.ThreadID };

            newFrame.Parent = Current;
            Current.Children.Add(newFrame);

            Current = newFrame;
            return newFrame;
        }

        internal void LeaveUnmanagedTransition(UnmanagedTransitionArgs args, IMethodInfoInternal method)
        {
            if (method.WasUnknown && !includeUnknownTransitions)
            {
                ValidateSequence(args);
                return;
            }

            ValidateEnd(args, method);

            EndCallInternal();
        }

        #endregion
        #region Exception

        public void Exception(ExceptionArgs args)
        {
            Exceptions[args.Sequence] = new ExceptionInfo(args, ThreadId, Current);
        }

        internal void ExceptionFrameUnwind(CallArgs args, IMethodInfoInternal method)
        {
            if (args.UnwindFrameKind != FrameKind.Managed)
            {
                if (method.WasUnknown && !includeUnknownTransitions)
                {
                    //We never recorded this frame in the first place, so we don't need to unwind it
                    ValidateSequence(args);
                    return;
                }
            }

            ValidateEnd(args, method);

            EndCallInternal();
        }

        public void ExceptionCompleted(ExceptionCompletedArgs args)
        {
            //When we start the target process, we SHOULD get notified of every exception that is created (and completed)
            //We saw a case with Visual Studio however where we got a completed event for an exception we had never seen. Maybe the initial
            //creation event was dropped due to high load. The only other scenario where we can hit this should be where a session was created
            //globally without the use of our profiler controller, and we globally attach to it
            if (Exceptions.TryGetValue(args.Sequence, out var exception))
            {
                exception.Status = args.Reason;
                exception.HandledFrame = Current;
            }
        }

        #endregion

        private void EndCallInternal()
        {
            if (!(Current is IRootFrame))
                Current = Current.Parent;
        }

        private void ValidateSequence(ICallArgs args)
        {
            var expectedNextSequence = lastSequence + 1;

            if (lastSequence != 0 && expectedNextSequence != args.Sequence)
            {
                throw new InvalidOperationException($"Expected sequence: {expectedNextSequence}. Actual: {args.Sequence}. This indicates either an exception occurred in the profiler that messed up the bookkeeping, " +
                                                    $"or ETW dropped an event due to too many events being generated. Consider using synchronous mode to determine whether ETW dropped the event. " +
                                                    $"Note that the profiler did not detect any frames were lost. If it did, {PROFILER_HRESULT.PROFILER_E_UNKNOWN_FRAME} would have been detected in ProfilerSession.Validate()");
            }   

            lastSequence = args.Sequence;
        }

        private void ValidateEnd(ICallArgs args, IMethodInfo method)
        {
            ValidateSequence(args);

            if (Current is IMethodFrame f && method != null)
            {
                var expected = f.MethodInfo;

                if (expected.FunctionID.Value != method.FunctionID.Value)
                    throw new InvalidOperationException($"Expected method: {expected} ({expected.FunctionID:X}). Actual: {method} ({method.FunctionID:X})");
            }
        }

        public override string ToString()
        {
            return Root.ThreadName ?? Root.ThreadId.ToString();
        }
    }
}
