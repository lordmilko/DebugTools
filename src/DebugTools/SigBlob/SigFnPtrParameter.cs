namespace DebugTools
{
    public class SigFnPtrParameter : ISigParameter
    {
        public SigType Type { get; }

        internal SigFnPtrParameter(SigType type)
        {
            Type = type;
        }

        public override string ToString()
        {
            return Type.ToString();
        }
    }
}
