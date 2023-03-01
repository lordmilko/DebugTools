using System;
using ClrDebug;

namespace DebugTools
{
    [Serializable]
    public class SymbolAndModuleInfo
    {
        public SymFromAddrResult Symbol { get; }

        public SymbolModule Module { get; }

        public CLRDATA_ADDRESS RVA { get; }

        public SymbolAndModuleInfo(SymFromAddrResult symbol, SymbolModule module)
        {
            Symbol = symbol;
            Module = module;
            RVA = symbol.SymbolInfo.Address + (ulong )symbol.Displacement - Module.Start;
        }

        public override string ToString()
        {
            return $"{Module}!{Symbol}";
        }
    }
}
