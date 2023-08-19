using System;
using System.Linq;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;

namespace DebugTools.Ui
{
    class CustomUiElement : AutomationElement, IUiElement
    {
        public new IUiElement Parent => UiElement.New(base.Parent);

        public IUiElement[] Children => FindAllChildren().Select(UiElement.New).ToArray();

        public IntPtr Handle => UiElement.GetProperty(Properties.NativeWindowHandle);

        public CustomUiElement(FrameworkAutomationElementBase frameworkAutomationElement) : base(frameworkAutomationElement)
        {
        }
    }
}
