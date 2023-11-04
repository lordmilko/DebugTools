using System.Management.Automation;
using DebugTools.Dynamic;
using DebugTools.Host;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "DbgProxy")]
    public class GetDbgProxy : PSCmdlet
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public StaticFieldInfo Field { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Dbg { get; set; }

        private InjectedHostSession session;

        protected override void ProcessRecord()
        {
            session = DebugToolsSessionState.Services.GetOrCreate<InjectedHostSession>(Field.Process, Dbg);

            var remoteStub = session.App.CreateRemoteStub(Field);

            var localValue = LocalProxyStub.Wrap(remoteStub, PowerShellLocalProxyNotifier.Instance);

            WriteObject(localValue);
        }
    }
}
