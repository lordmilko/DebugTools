using System.Diagnostics;
using DebugTools.Tracing;

namespace DebugTools.Profiler
{
    [DebuggerDisplay("Type = {Type,nq}, Status = {Status}, Sequence = {Sequence}")]
    public class ExceptionInfo
    {
        public string Type { get; }

        public long Sequence { get; }

        public ExceptionStatus Status { get; set; }

        public ExceptionInfo(ExceptionArgs args)
        {
            Type = args.Type;
            Sequence = args.Sequence;
        }

        public override string ToString()
        {
            return Type;
        }
    }
}
