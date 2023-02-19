using System;

namespace DebugTools
{
    [Serializable]
    public unsafe struct SymFromAddrResult
    {
        public long Displacement { get; }

        public SymbolInfo SymbolInfo { get; }

        public SymFromAddrResult(long displacement, SymbolInfo symbolInfo)
        {
            Displacement = displacement;
            SymbolInfo = symbolInfo;
        }

        public override string ToString()
        {
            if (Displacement == 0)
                return SymbolInfo.ToString();

            return $"{SymbolInfo}+{Displacement:X}";
        }
    }
}
