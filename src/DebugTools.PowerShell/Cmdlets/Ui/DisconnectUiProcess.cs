using System.Management.Automation;
using DebugTools.Ui;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommunications.Disconnect, "UiProcess")]
    public class DisconnectUiProcess : DebugToolsCmdlet
    {
        [Parameter(Mandatory = false, ValueFromPipeline = true)]
        public UiSession Session { get; set; }

        protected override void ProcessRecord()
        {
            if (Session == null)
                Session = DebugToolsSessionState.GetImplicitUiSession(false);

            if (Session != null)
            {
                DebugToolsSessionState.UiSessions.Remove(Session);
                Session.Dispose();
            }
        }
    }
}
