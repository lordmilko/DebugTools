using ClrDebug;

namespace DebugTools
{
    public class SigParameter : ISigParameter
    {
        public SigType Type { get; }

        public GetParamPropsResult? Info { get; }

        internal SigParameter(SigType type, GetParamPropsResult? info)
        {
            Type = type;
            Info = info;
        }

        public override string ToString()
        {
            if (Info == null)
                return Type.ToString();

            return $"{Type} {Info.Value.szName}";
        }
    }
}