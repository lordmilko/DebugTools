namespace DebugTools
{
    public class SigVarArgParameter : ISigParameter
    {
        public SigType Type { get; }

        internal SigVarArgParameter(SigType type)
        {
            Type = type;
        }

        public override string ToString()
        {
            return Type.ToString();
        }
    }
}