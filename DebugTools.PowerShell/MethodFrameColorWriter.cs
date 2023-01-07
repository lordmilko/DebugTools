using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using DebugTools.Profiler;

namespace DebugTools.PowerShell
{
    public class MethodFrameColorWriter : IFormattedMethodFrameWriter
    {
        private MethodFrameFormatter formatter;

        public ConcurrentDictionary<object, byte> HighlightValues { get; set; }

        public WildcardPattern[] HighlightMethodNames { get; set; }

        public List<IFrame> HighlightFrames { get; set; }

        public IOutputSource Output { get; }

        public MethodFrameColorWriter(MethodFrameFormatter formatter, IOutputSource output)
        {
            this.formatter = formatter;
            Output = output;
        }

        private StringBuilder nameBuilder = new StringBuilder();
        private bool buildingName = false;

        public IMethodFrameWriter Write(object value, IFrame frame, FrameTokenKind kind)
        {
            bool highlightFrame = HighlightFrames.Contains(frame);

            if (value is FormattedValue f)
            {
                if (HighlightValues?.ContainsKey(f.Original) == true)
                    Output.WriteColor(f.Formatted, ConsoleColor.Yellow);
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
                            Output.WriteColor(str, ConsoleColor.Green);
                        else
                            WriteMaybeHighlight(str, highlightFrame);

                        break;

                    case FrameTokenKind.Parameter:
                    case FrameTokenKind.ReturnValue:
                        if (HighlightValues?.ContainsKey(value) == true)
                            Output.WriteColor(value, ConsoleColor.Yellow);
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

        private void WriteMaybeHighlight(object message, bool highlightFrame)
        {
            if (highlightFrame)
                Output.WriteColor(message, ConsoleColor.Green);
            else
                Output.Write(message);
        }

        public void Print(IFrame frame)
        {
            formatter.Format(frame, this);
        }

        private bool ShouldHighlightMethod(string str)
        {
            if (HighlightMethodNames == null)
                return false;

            return HighlightMethodNames.Any(h => h.IsMatch(str));
        }
    }
}
