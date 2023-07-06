using System;
using System.Management.Automation;
using DebugTools.Ui;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace DebugTools.PowerShell.Cmdlets.Ui
{
    [Cmdlet(VerbsLifecycle.Invoke, "UiElement")]
    public class InvokeUiElement : UiCmdlet
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public IUiElement Element { get; set; }

        protected override void ProcessRecordEx()
        {
            if (Element is ButtonUiElement b)
                b.Invoke();
            else if (Element is TreeItem ti)
                ti.Select();
            else if (Element is ListBoxItem li)
            {
                if (li.IsSelected)
                    li.RemoveFromSelection();
                else
                    li.AddToSelection();
            }
            else if (Element is MenuItem mi)
            {
                var state = mi.Patterns.ExpandCollapse.Pattern.ExpandCollapseState.Value;

                switch (state)
                {
                    case ExpandCollapseState.Collapsed:
                        mi.Expand();
                        break;

                    case ExpandCollapseState.Expanded:
                        mi.Collapse();
                        break;

                    case ExpandCollapseState.LeafNode:
                        throw new InvalidOperationException($"{nameof(MenuItem)} '{Element.Name}' is of type {state} and cannot be expanded.");

                    default:
                        throw new NotImplementedException($"Don't know how to handle {nameof(MenuItem)} in state {state}");
                }
            }
            else
                throw new InvalidOperationException($"Don't know how to invoke element of type {Element.ControlType}");
        }
    }
}
