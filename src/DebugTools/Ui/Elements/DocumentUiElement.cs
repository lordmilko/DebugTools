using System.Linq;
using System.Text;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;

namespace DebugTools.Ui
{
    //Text is typically stored in ValuePattern, which TextBox can retrieve
    class DocumentUiElement : TextBox, IUiElement
    {
        public new string Name => UiElement.GetProperty(Properties.Name);

        public new IUiElement Parent => UiElement.New(base.Parent);

        public IUiElement[] Children => FindAllChildren().Select(UiElement.New).ToArray();

        public new string ItemStatus => UiElement.GetProperty(Properties.ItemStatus);

        public DocumentUiElement(FrameworkAutomationElementBase frameworkAutomationElement) : base(frameworkAutomationElement)
        {
        }

        public new string Text => SanitizeText(base.Text);

        public string RawText => base.Text;

        private string SanitizeText(string text)
        {
            var chars = text.ToCharArray();

            StringBuilder builder = null;

            int i = 0;

            void InitializeBad()
            {
                if (builder == null)
                {
                    builder = new StringBuilder();

                    for (var j = 0; j <= i; j++)
                        builder.Append(chars[j]);
                }
                else
                    builder.Append(chars[i]);
            }

            for (; i < chars.Length; i++)
            {
                var c = chars[i];

                if (c == '\r')
                {
                    if (i == chars.Length - 1 || chars[i + 1] != '\n')
                    {
                        InitializeBad();
                        builder.Append('\n');
                    }
                    else
                    {
                        builder?.Append(c);
                    }
                }
                else
                {
                    builder?.Append(c);
                }
            }

            return builder?.ToString() ?? text;
        }
    }
}
