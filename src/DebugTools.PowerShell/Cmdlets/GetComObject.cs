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
            string[] interfaceNameRegexes = null;

            if (InterfaceName != null)
            {
                var wildcards = InterfaceName.Select(n => new WildcardPattern(n, WildcardOptions.IgnoreCase)).ToArray();

                var property = typeof(WildcardPattern).GetProperty("PatternConvertedToRegex", BindingFlags.Instance | BindingFlags.NonPublic);

                if (property == null)
                    throw new InvalidOperationException("Could not find property PatternConvertedToRegex");

                interfaceNameRegexes = wildcards.Select(w => (string) property.GetValue(w)).ToArray();
            }

            IEnumerable<DbgVtblSymbolInfo> results = HostApp.GetComObjects(Process.Id, interfaceNameRegexes);

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
