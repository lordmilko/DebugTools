using System.Reflection;

namespace DebugTools.Dynamic
{
    struct ParameterInfoAndRank
    {
        public ParameterInfo Info { get; }

        public ConversionRank? Rank { get; }

        public ParameterInfoAndRank(ParameterInfo info, ConversionRank? rank)
        {
            Info = info;
            Rank = rank;
        }
    }
}
