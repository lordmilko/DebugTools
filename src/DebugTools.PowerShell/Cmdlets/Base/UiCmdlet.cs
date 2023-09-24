using System.Management.Automation;
using DebugTools.Ui;

namespace DebugTools.PowerShell.Cmdlets
{
    public abstract class UiCmdlet : DebugToolsCmdlet
    {
        [Parameter(Mandatory = false, ValueFromPipeline = true)]
        public LocalUiSession Session { get; set; }

        protected override void ProcessRecord()
        {
            if (Session == null)
                Session = DebugToolsSessionState.Services.GetImplicitSubSession<LocalUiSession>();

            ProcessRecordEx();
        }

        protected abstract void ProcessRecordEx();
    }
}
