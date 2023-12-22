using System;
using ChaosLib;
using ClrDebug;

namespace DebugTools
{
    [Serializable]
    public abstract class DbgSymbolInfo
    {
        public SymbolModule Module { get; }

        public SymFromAddrResult Symbol { get; }

        public CLRDATA_ADDRESS RVA { get; }

        public CLRDATA_ADDRESS LoadedAddress { get; }

        public CLRDATA_ADDRESS OriginalAddress { get; }

        protected DbgSymbolInfo(SymbolAndModuleInfo symbolAndModule)
        {
            Symbol = symbolAndModule.Symbol;
            Module = symbolAndModule.Module;
            RVA = symbolAndModule.RVA;
            LoadedAddress = symbolAndModule.Module.Start + RVA;
            OriginalAddress = symbolAndModule.Module.OriginalBase + RVA;
        }
    }
}
