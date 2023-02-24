using System.Diagnostics;
using DebugTools.Tracing;

namespace DebugTools.Profiler
{
    [DebuggerDisplay("Type = {Type,nq}, Status = {Status}, Sequence = {Sequence}")]
    public class ExceptionInfo
    {
        public string Type { get; }

        public long Sequence { get; }

        public int ThreadId { get; }

        public ExceptionStatus Status { get; set; }

        public IMethodFrame ThrownFrame { get; }

        public IMethodFrame HandledFrame { get; internal set; }

        public ExceptionInfo(ExceptionArgs args, int threadId, IMethodFrame thrownFrame)
        {
            Type = args.Type;
            Sequence = args.Sequence;
            ThreadId = threadId;
            ThrownFrame = thrownFrame;
        }

        public override string ToString()
        {
            return Type;
        }
    }
}
