using System;
using System.Linq;
using Microsoft.Diagnostics.Runtime;

namespace DebugTools
{
    [Serializable]
    public class DbgVtblSymbolInfo : DbgSymbolInfo
    {
        public string[] Interfaces { get; }

        public DbgMethodSymbolInfo[] Methods { get; }

        public DbgVtblSymbolInfo(SymbolAndModuleInfo vtbl, SymbolAndModuleInfo[] methods, RcwData rcwData) : base(vtbl)
        {
            Methods = methods.Select(m => new DbgMethodSymbolInfo(m)).ToArray();

            Interfaces = rcwData.Interfaces.Select(i => CleanInterfaceName(i.Type.Name)).OrderBy(v => v).ToArray();
        }

        internal static string CleanInterfaceName(string name)
        {
            if (name.Contains('+'))
                return name;

            var dot = name.LastIndexOf('.');

            if (dot != -1)
                return name.Substring(dot + 1);

            return name;
        }
    }
}
