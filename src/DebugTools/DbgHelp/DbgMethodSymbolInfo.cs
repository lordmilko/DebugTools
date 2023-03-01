using System;

namespace DebugTools
{
    [Serializable]
    public class DbgMethodSymbolInfo : DbgSymbolInfo
    {
        public DbgMethodSymbolInfo(SymbolAndModuleInfo method) : base(method)
        {
        }
    }
}
