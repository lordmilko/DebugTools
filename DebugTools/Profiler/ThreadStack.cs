using System;
using System.Collections.Generic;
using ClrDebug;
using DebugTools.Tracing;

namespace DebugTools.Profiler
{
    public class ThreadStack
    {
        public IFrame Current { get; set; }

        public RootFrame Root => Current.GetRoot();

        public Dictionary<long, ExceptionInfo> Exceptions { get; } = new Dictionary<long, ExceptionInfo>();

        private long lastSequence;

        #region CallArgs

        public void Enter(CallArgs args, MethodInfo method)
        {
            Validate(args);

            var newFrame = new MethodFrame(method, args.Sequence);

            if (Current == null)
                Current = new RootFrame { ThreadId = args.ThreadID };

            newFrame.Parent = Current;
            Current.Children.Add(newFrame);

            Current = newFrame;
        }

        public void Leave(CallArgs args, MethodInfo method)
        {
            ValidateEnd(args, method);

            EndCallInternal();
        }

        public void Tailcall(CallArgs args, MethodInfo method)
        {
            ValidateEnd(args, method);

            //As per Dave Broman's blog, the correct way to handle a tailcall is exactly the same way you would handle a leave call
            //https://web.archive.org/web/20190110160434/https://blogs.msdn.microsoft.com/davbr/2007/06/20/enter-leave-tailcall-hooks-part-2-tall-tales-of-tail-calls/
            EndCallInternal();
        }

        #endregion
        #region CallArgsDetailed

        public void EnterDetailed(CallDetailedArgs args, MethodInfo method)
        {
            Validate(args);

            var newFrame = new MethodFrameDetailed(method, args);

            if (Current == null)
                Current = new RootFrame { ThreadId = args.ThreadID };

            newFrame.Parent = Current;
            Current.Children.Add(newFrame);

            Current = newFrame;
        }

        public void LeaveDetailed(CallDetailedArgs args, MethodInfo method)
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

        public void TailcallDetailed(CallDetailedArgs args, MethodInfo method)
        {
            ValidateEnd(args, method);

            EndCallInternal();
        }

        #endregion
        #region Exception

        public void Exception(ExceptionArgs args)
        {
            Exceptions[args.Sequence] = new ExceptionInfo(args);
        }

        public void ExceptionFrameUnwind(CallArgs args, MethodInfo method)
        {
            ValidateEnd(args, method);

            EndCallInternal();
        }

        public void ExceptionCompleted(ExceptionCompletedArgs args)
        {
            Exceptions[args.Sequence].Status = args.Reason;
        }

        #endregion

        private void EndCallInternal()
        {
            if (!(Current is RootFrame))
                Current = Current.Parent;
        }

        private void Validate(ICallArgs args)
        {
            var expectedNextSequence = lastSequence + 1;

            if (lastSequence != 0 && expectedNextSequence != args.Sequence)
                throw new InvalidOperationException($"Expected sequence: {expectedNextSequence}. Actual: {args.Sequence}");

            lastSequence = args.Sequence;
        }

        private void ValidateEnd(ICallArgs args, MethodInfo method)
        {
            Validate(args);

            if (Current is MethodFrame f && method != null)
            {
                var expected = f.MethodInfo;

                if (expected != method)
                    throw new InvalidOperationException($"Expected method: {expected} ({expected.FunctionID:X}). Actual: {method} ({method.FunctionID:X})");
            }
        }
    }
}
