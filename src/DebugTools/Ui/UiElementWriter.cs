using FlaUI.Core.AutomationElements;

namespace DebugTools.Ui
{
    class UiElementWriter : IFormattedUiElementWriter
    {
        public IOutputSource Output { get; }

        public UiElementWriter(IOutputSource output)
        {
            Output = output;
        }

        public void Print(IUiElement element)
        {
            AutomationElement elm = (AutomationElement) element;

            Output.Write($"[{elm.ControlType}] {elm.Name}");
        }
    }
}
