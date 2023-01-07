using System;

namespace DebugTools
{
    public interface IOutputSource
    {
        void Write(object value);

        void WriteLine();

        void WriteColor(object value, ConsoleColor color);
    }
}
