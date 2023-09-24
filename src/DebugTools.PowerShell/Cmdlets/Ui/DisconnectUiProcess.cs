using System.Management.Automation;
using DebugTools.Ui;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommunications.Disconnect, "UiProcess")]
    public class DisconnectUiProcess : DebugToolsCmdlet
    {
        [Parameter(Mandatory = false, ValueFromPipeline = true)]
        public LocalUiSession Session { get; set; }

        protected override void ProcessRecord()
        {
            if (Session == null)
                Session = DebugToolsSessionState.Services.GetImplicitSubSession<LocalUiSession>(false);

            if (Session != null)
                DebugToolsSessionState.Services.Close(Session.Process.Id, Session);
        }
    }
}
