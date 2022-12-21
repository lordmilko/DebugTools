namespace DebugTools
{
    public class SigArgListParameter : ISigParameter
    {
        public SigType Type { get; }

        public override string ToString()
        {
            return "__arglist";
        }
    }
}