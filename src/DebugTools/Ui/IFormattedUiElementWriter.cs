namespace DebugTools.Ui
{
    interface IFormattedUiElementWriter
    {
        IOutputSource Output { get; }

        void Print(IUiElement element);
    }
}