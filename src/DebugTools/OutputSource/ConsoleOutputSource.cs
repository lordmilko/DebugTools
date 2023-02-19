using System;

namespace DebugTools
{
    class ConsoleOutputSource : IOutputSource
    {
        public void Write(object value) => Console.Write(value);

        public void WriteLine() => Console.WriteLine();

        public void WriteColor(object value, ConsoleColor color)
        {
            ConsoleColor fg = Console.ForegroundColor;

            Console.ForegroundColor = color;

            try
            {
                Console.Write(value);
            }
            finally
            {
                Console.ForegroundColor = fg;
            }
        }
    }
}
