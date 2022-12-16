using System.Management.Automation;

namespace DebugTools.PowerShell.Cmdlets
{
    public abstract class StackFrameCmdlet : ProfilerSessionCmdlet
    {
        [Parameter(Mandatory = false, ValueFromPipeline = true)]
        public IFrame Frame { get; set; }

        protected override void ProcessRecordEx()
        {
            if (Frame != null)
                DoProcessRecordEx();
            else
            {
                var frames = Session.LastTrace;

                foreach (var frame in frames)
                {
                    Frame = frame.Root;
                    DoProcessRecordEx();
                }
            }
        }

        protected abstract void DoProcessRecordEx();
    }
}