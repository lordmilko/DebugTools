using System.Linq;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;

namespace DebugTools.Ui
{
    class ButtonUiElement : Button, IUiElement
    {
        public new IUiElement Parent => UiElement.New(base.Parent);

        public IUiElement[] Children => FindAllChildren().Select(UiElement.New).ToArray();

        public new string AutomationId => UiElement.GetProperty(Properties.AutomationId);
        public new string ClassName => UiElement.GetProperty(Properties.ClassName);
        public new string ItemStatus => UiElement.GetProperty(Properties.ItemStatus);

        public ButtonUiElement(FrameworkAutomationElementBase frameworkAutomationElement) : base(frameworkAutomationElement)
        {
        }
    }
}
