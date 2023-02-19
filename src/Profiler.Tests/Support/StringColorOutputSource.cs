using System;
using System.Text;
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
            var str = builder.ToString();
            builder.Clear();
            return str;
        }

        public override string ToString()
        {
            return builder.ToString();
        }
    }
}
