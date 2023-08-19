using System;
using System.Linq;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;

namespace DebugTools.Ui
{
    class ListItemUiElement : ListBoxItem, IUiElement
    {
        public new IUiElement Parent => UiElement.New(base.Parent);

        public IUiElement[] Children => FindAllChildren().Select(UiElement.New).ToArray();

        public IntPtr Handle => UiElement.GetProperty(Properties.NativeWindowHandle);

        public new string AutomationId => UiElement.GetProperty(Properties.AutomationId);
        public new string ClassName => UiElement.GetProperty(Properties.ClassName);
        public new string ItemStatus => UiElement.GetProperty(Properties.ItemStatus);

        public ListItemUiElement(FrameworkAutomationElementBase frameworkAutomationElement) : base(frameworkAutomationElement)
        {
        }
    }
}
