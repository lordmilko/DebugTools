using System.Management.Automation;
using DebugTools.Ui;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Show, "UiTree")]
    public class ShowUiTree : UiCmdlet
    {
        [Parameter(Mandatory = false, ValueFromPipeline = true)]
        public IUiElement Root { get; set; }

        protected override void ProcessRecordEx()
        {
            if (Root == null)
                Root = Session.Root;

            var writer = new UiTreeWriter(
                new UiElementWriter(new ConsoleOutputSource()),
                CancellationToken
            );

            writer.Execute(Root);
        }
    }
}
