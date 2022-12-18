using System.Management.Automation;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Invoke, "DbgProfilerCommand")]
    public class InvokeDbgProfilerCommand : ProfilerSessionCmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        public MessageType MessageType { get; set; }

        [Parameter(Mandatory = true, Position = 1)]
        public object Value { get; set; }

        protected override void ProcessRecordEx()
        {
            Session.ExecuteCommand(MessageType, Value);
        }
    }
}