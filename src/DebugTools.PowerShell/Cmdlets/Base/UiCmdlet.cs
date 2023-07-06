using System.Management.Automation;
using DebugTools.Ui;

namespace DebugTools.PowerShell.Cmdlets
{
    public abstract class UiCmdlet : DebugToolsCmdlet
    {
        [Parameter(Mandatory = false, ValueFromPipeline = true)]
        public UiSession Session { get; set; }

        protected override void ProcessRecord()
        {
            if (Session == null)
                Session = DebugToolsSessionState.GetImplicitUiSession();

            ProcessRecordEx();
        }

        protected abstract void ProcessRecordEx();
    }
}
