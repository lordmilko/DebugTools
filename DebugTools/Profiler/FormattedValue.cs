namespace DebugTools.Profiler
{
    class FormattedValue
    {
        public object Original { get; }

        public string Formatted { get; }

        public FormattedValue(object original, string formatted)
        {
            Original = original;
            Formatted = formatted;
        }

        public override string ToString()
        {
            return Formatted;
        }
    }
}