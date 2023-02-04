using System;

namespace DebugTools
{
    [Serializable]
    public class DbgVtblSymbolInfo
    {
        public SymbolModule Module { get; }

        public SymFromAddrResult Symbol { get; }

        public ulong RVA { get; }

        public DbgSymbolInfo[] Methods { get; }

        public DbgVtblSymbolInfo(DbgSymbolInfo vtbl, DbgSymbolInfo[] methods)
        {
            Symbol = vtbl.Symbol;
            Module = vtbl.Module;
            RVA = vtbl.RVA;

            Methods = methods;
        }
    }
}
