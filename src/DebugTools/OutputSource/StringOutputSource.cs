using System;
using System.Text;

namespace DebugTools
{
    class StringOutputSource : IOutputSource
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
            builder.Append(value);
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
