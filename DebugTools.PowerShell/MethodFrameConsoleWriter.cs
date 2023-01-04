using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using DebugTools.Profiler;

namespace DebugTools.PowerShell
{
    class MethodFrameConsoleWriter : IMethodFrameWriter
    {
        private MethodFrameFormatter formatter;

        public HashSet<object> HighlightValues { get; set; }

        public WildcardPattern[] HighlightMethods { get; set; }

        public List<IFrame> HighlightFrames { get; set; }

        public MethodFrameConsoleWriter(MethodFrameFormatter formatter)
        {
            this.formatter = formatter;
        }

        private StringBuilder nameBuilder = new StringBuilder();
        private bool buildingName = false;

        public IMethodFrameWriter Write(object value, IFrame frame, FrameTokenKind kind)
        {
            bool highlightFrame = HighlightFrames.Contains(frame);

            if (value is FormattedValue f)
            {
                if (HighlightValues.Contains(f.Original))
                    WriteColor(f.Formatted, ConsoleColor.Yellow);
                else
                    WriteMaybeHighlight(f.Formatted, highlightFrame);
            }
            else
            {
                switch (kind)
                {
                    case FrameTokenKind.TypeName:
                        buildingName = true;
                        nameBuilder.Append(value);
                        break;

                    case FrameTokenKind.MethodName:
                        buildingName = false;
                        nameBuilder.Append(value);
                        var str = nameBuilder.ToString();
                        nameBuilder.Clear();

                        if (ShouldHighlightMethod(str))
                            WriteColor(str, ConsoleColor.Green);
                        else
                            WriteMaybeHighlight(str, highlightFrame);

                        break;

                    case FrameTokenKind.Parameter:
                    case FrameTokenKind.ReturnValue:
                        if (HighlightValues.Contains(value))
                            WriteColor(value, ConsoleColor.Yellow);
                        else
                            WriteMaybeHighlight(value, highlightFrame);
                        break;

                    default:
                        if (buildingName)
                            nameBuilder.Append(value);
                        else
                            WriteMaybeHighlight(value, highlightFrame);
                        break;
                }
            }

            return this;
        }

        private void WriteColor(object message, ConsoleColor color)
        {
            ConsoleColor fg = Console.ForegroundColor;

            Console.ForegroundColor = color;

            try
            {
                Console.Write(message);
            }
            finally
            {
                Console.ForegroundColor = fg;
            }
        }

        private void WriteNormal(object message) => Console.Write(message);

        private void WriteMaybeHighlight(object message, bool highlightFrame)
        {
            if (highlightFrame)
                WriteColor(message, ConsoleColor.Green);
            else
                WriteNormal(message);
        }

        public void Print(IFrame frame)
        {
            formatter.Format(frame, this);
        }

        private bool ShouldHighlightMethod(string str)
        {
            if (HighlightMethods == null)
                return false;

            return HighlightMethods.Any(h => h.IsMatch(str));
        }
    }
}
