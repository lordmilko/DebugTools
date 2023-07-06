using System;
using System.Management.Automation;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Find, "UiElement")]
    public class FindUiElement : UiCmdlet
    {
        protected override void ProcessRecordEx()
        {
            var result = WindowSelector.Execute();

            if (result != IntPtr.Zero)
                WriteObject(Session.FromHandle(result));
        }
    }
}
