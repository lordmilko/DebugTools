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

        /// <summary>
        /// Gets the frame that the exception was thrown from. If the exception was thrown on a frame that is ignored by the profiler, this will be the parent frame that is traced,
        /// or the <see cref="IRootFrame"/> if no higher frames exist on the thread.
        /// </summary>
        public IFrame ThrownFrame { get; }

        /// <summary>
        /// Gets the frame that the exception was handled in. If the exception was thrown on a frame that is ignored by the profiler, this will be the parent frame that is traced,
        /// or the <see cref="IRootFrame"/> if no higher frames exist on the thread.
        /// </summary>
        public IFrame HandledFrame { get; internal set; }

        public ExceptionInfo(ExceptionArgs args, int threadId, IFrame thrownFrame)
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
