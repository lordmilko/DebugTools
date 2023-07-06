using System.Threading;

namespace DebugTools.Ui
{
    class UiTreeWriter
    {
        private IFormattedUiElementWriter writer;

        private CancellationToken cancellationToken;

        public UiTreeWriter(IFormattedUiElementWriter writer, CancellationToken cancellationToken)
        {
            this.writer = writer;
            this.cancellationToken = cancellationToken;
        }

        public void Execute(params IUiElement[] elements)
        {
            foreach (var element in elements)
                Print(element, 0, string.Empty, false, true);
        }

        private void Print(IUiElement item, int level, string indent, bool last, bool first = false)
        {
            cancellationToken.ThrowIfCancellationRequested();

            writer.Output.Write(indent);

            if (!first)
            {
                if (last)
                {
                    writer.Output.Write("└─");
                    indent += "  ";
                }
                else
                {
                    writer.Output.Write("├─");
                    indent += "│ ";
                }
            }

            writer.Print(item);

            writer.Output.WriteLine();

            var children = item.Children;

            if (children.Length > 0)
            {
                for (var i = 0; i < children.Length; i++)
                    Print(children[i], level + 1, indent, i == children.Length - 1);
            }
        }
    }
}
