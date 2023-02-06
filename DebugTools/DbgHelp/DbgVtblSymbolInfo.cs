using System;
using System.Linq;
using ClrDebug;
using Microsoft.Diagnostics.Runtime;

namespace DebugTools
{
    [Serializable]
    public class DbgVtblSymbolInfo
    {
        public SymbolModule Module { get; }

        public SymFromAddrResult Symbol { get; }

        public CLRDATA_ADDRESS RVA { get; }

        public DbgSymbolInfo[] Methods { get; }

        public string[] Interfaces { get; }

        public DbgVtblSymbolInfo(DbgSymbolInfo vtbl, DbgSymbolInfo[] methods, RcwData rcwData)
        {
            Symbol = vtbl.Symbol;
            Module = vtbl.Module;
            RVA = vtbl.RVA;

            Methods = methods;

            Interfaces = rcwData.Interfaces.Select(i => i.Type.Name).ToArray();
        }
    }
}
