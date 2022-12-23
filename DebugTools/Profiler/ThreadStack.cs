using DebugTools.Tracing;

namespace DebugTools.Profiler
{
    public class ThreadStack
    {
        public IFrame Current { get; set; }

        public RootFrame Root => Current.GetRoot();

        public void AddMethod(CallArgs args, MethodInfo method)
        {
            var newFrame = new MethodFrame
            {
                MethodInfo = method
            };

            if (Current == null)
                Current = new RootFrame {ThreadId = args.ThreadID};

            newFrame.Parent = Current;
            Current.Children.Add(newFrame);

            Current = newFrame;
        }

        public void AddMethodDetailed(CallDetailedArgs args, MethodInfo method)
        {
            var newFrame = new MethodFrameDetailed
            {
                MethodInfo = method,
                Value = args.Value
            };

            if (Current == null)
                Current = new RootFrame { ThreadId = args.ThreadID };

            newFrame.Parent = Current;
            Current.Children.Add(newFrame);

            Current = newFrame;
        }

        public void EndCall()
        {
            if (!(Current is RootFrame))
                Current = Current.Parent;
        }

        public void EndCallDetailed(CallDetailedArgs args)
        {
            EndCall();
        }

        public void Tailcall(CallArgs args, MethodInfo method)
        {
            //As per Dave Broman's blog, the correct way to handle a tailcall is exactly the same way you would handle a leave call
            //https://web.archive.org/web/20190110160434/https://blogs.msdn.microsoft.com/davbr/2007/06/20/enter-leave-tailcall-hooks-part-2-tall-tales-of-tail-calls/
            EndCall();
        }

        public void TailcallDetailed(CallDetailedArgs args, MethodInfo method)
        {
            EndCall();
        }
    }
}
