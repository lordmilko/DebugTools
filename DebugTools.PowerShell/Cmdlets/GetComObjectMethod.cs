using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "ComObjectMethod")]
    public class GetComObjectMethod : HostCmdlet
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public DbgVtblSymbolInfo DbgVtblSymbolInfo { get; set; }

        [Parameter(Mandatory = false, Position = 0)]
        public string[] Name { get; set; }

        protected override void ProcessRecordEx()
        {
            IEnumerable<DbgSymbolInfo> methods = DbgVtblSymbolInfo.Methods;

            if (Name != null)
            {
                var wildcards = Name.Select(n => new WildcardPattern(n, WildcardOptions.IgnoreCase)).ToArray();

                methods = methods.Where(m => wildcards.Any(w => w.IsMatch(m.Symbol.ToString())));
            }

            foreach (var method in methods)
                WriteObject(method);
        }
    }
}
