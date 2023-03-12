using System.Linq;
using System.Management.Automation;
using DebugTools.Profiler;

namespace DebugTools.PowerShell.Cmdlets
{
    public abstract class StackFrameCmdlet : FilterStackFrameCmdlet
    {
        [Parameter(Mandatory = false, ValueFromPipeline = true)]
        public IFrame Frame { get; set; }

        [Parameter(Mandatory = false)]
        public int[] ThreadId { get; set; }

        protected override void ProcessRecordEx()
        {
            if (Frame != null && !ShouldIgnore())
                DoProcessRecordEx();
            else
            {
                var frames = Session.LastTrace;

                foreach (var frame in frames)
                {
                    Frame = frame.Root;

                    if (ShouldIgnore())
                        continue;

                    DoProcessRecordEx();
                }
            }
        }

        protected abstract void DoProcessRecordEx();

        private bool ShouldIgnore()
        {
            if (MyInvocation.BoundParameters.ContainsKey(nameof(ThreadId)))
            {
                var id = Frame.GetRoot().ThreadId;

                if (!ThreadId.Any(t => t == id))
                    return true;
            }

            return false;
        }
    }
}
