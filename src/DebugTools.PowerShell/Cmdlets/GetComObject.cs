using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "ComObject")]
    public class GetComObject : HostCmdlet
    {
        [Parameter(Mandatory = false, Position = 0)]
        public string[] InterfaceName { get; set; }

        [Parameter(Mandatory = false)]
        public string[] SymbolName { get; set; }

        protected override void ProcessRecordEx()
        {
            var interfaceNameRegex = GetWildcardRegex(InterfaceName);

            IEnumerable<DbgVtblSymbolInfo> results = HostApp.GetComObjects(Process.Id, interfaceNameRegex);

            if (SymbolName != null)
            {
                var wildcards = SymbolName.Select(n => new WildcardPattern(n, WildcardOptions.IgnoreCase)).ToArray();

                results = results.Where(r => wildcards.Any(w => w.IsMatch(r.Symbol.SymbolInfo.Name)));
            }

            foreach (DbgVtblSymbolInfo result in results)
                WriteObject(result);
        }
    }
}
