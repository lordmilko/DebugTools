﻿using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "ComObject")]
    public class GetComObject : HostCmdlet
    {
        [Parameter(Mandatory = false, Position = 0)]
        public string[] Name { get; set; }

        protected override void ProcessRecordEx()
        {
            IEnumerable<DbgVtblSymbolInfo> results = HostApp.GetComObjects(Process.Id);

            if (Name != null)
            {
                var wildcards = Name.Select(n => new WildcardPattern(n, WildcardOptions.IgnoreCase)).ToArray();

                results = results.Where(m => wildcards.Any(w => 
                    w.IsMatch(m.Symbol.ToString()) || m.Interfaces.Any(w.IsMatch))
                );
            }

            foreach (DbgVtblSymbolInfo result in results)
                WriteObject(result);
        }
    }
}
