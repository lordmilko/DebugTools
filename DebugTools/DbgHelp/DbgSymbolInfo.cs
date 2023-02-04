using System;
using ClrDebug;

namespace DebugTools
{
    [Serializable]
    public class DbgSymbolInfo
    {
        public SymFromAddrResult Symbol { get; }

        public SymbolModule Module { get; }

        public CLRDATA_ADDRESS RVA { get; }

        public DbgSymbolInfo(SymFromAddrResult symbol, SymbolModule module)
        {
            Symbol = symbol;
            Module = module;
            RVA = symbol.SymbolInfo.Address - Module.Start;
        }

        public override string ToString()
        {
            return $"{Module}!{Symbol}";
        }
    }
}
