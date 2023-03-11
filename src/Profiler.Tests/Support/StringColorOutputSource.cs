using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DebugTools;

namespace Profiler.Tests
{
    class StringColorOutputSource : IOutputSource
    {
        private StringBuilder builder = new StringBuilder();

        public void Write(object value)
        {
            builder.Append(value);
        }

        public void WriteLine()
        {
            builder.AppendLine();
        }

        public void WriteColor(object value, ConsoleColor color)
        {
            builder.Append($"<{color}>{value}</{color}>");
        }

        public string ToStringAndClear()
        {
            var str = ToString();
            builder.Clear();
            return str;
        }

        public override string ToString()
        {
            var str = builder.ToString();

            var matches = Regex.Matches(str, "(?<close><\\/(?<tag1>.+?)>)(?<open><(?<tag2>.+?)>)");

            if (matches.Count > 0)
            {
                var stack = new Stack<string>();

                foreach (var match in matches.OfType<Match>().Reverse())
                {
                    var closeTag = match.Groups["close"];
                    var closeName = match.Groups["tag1"];
                    var openTag = match.Groups["open"];
                    var openName = match.Groups["tag2"];

                    //Different highlight colours; no need to replace
                    if (closeName.Value != openName.Value)
                    {
                        if (stack.Count == 0 || stack.Peek() != openName.Value)
                        {
                            //We're going backwards so we want to tag the close tag here, and the open tag later
                            var newPrefix = str.Substring(0, match.Index + closeTag.Length);
                            var newSuffix = str.Substring(openTag.Index + openTag.Length);

                            str = newPrefix + newSuffix;

                            stack.Push(closeName.Value);
                        }
                        else
                        {
                            stack.Pop();

                            var newPrefix = str.Substring(0, match.Index);
                            var newSuffix = str.Substring(openTag.Index);

                            str = newPrefix + newSuffix;

                            //str =  + str.Substring(match.Groups["close"].Index, match.Groups["close"].Length) + str.Substring(match.Groups["open"].Index);
                        }
                    }
                    else
                        str = str.Substring(0, match.Index) + str.Substring(match.Index + match.Length);
                }
            }

            return str;
        }
    }
}
