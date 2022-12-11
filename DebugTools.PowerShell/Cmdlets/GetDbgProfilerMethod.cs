using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "DbgProfilerMethod")]
    public class GetDbgProfilerMethod : ProfilerSessionCmdlet
    {
        [Parameter(Mandatory = false, Position = 0)]
        public string[] Name { get; set; }

        protected override void ProcessRecordEx()
        {
            IEnumerable<MethodInfo> methods = Session.Methods;

            if (Name != null)
            {
                var wildcards = Name.Select(n => new WildcardPattern(n, WildcardOptions.IgnoreCase)).ToArray();

                methods = methods.Where(m => wildcards.All(w =>
                {
                    if (w.IsMatch(m.MethodName))
                        return true;

                    if (w.IsMatch(m.TypeName))
                        return true;

                    if (w.IsMatch(m.ModuleName))
                        return true;

                    return false;
                }));
            }

            WriteObject(methods, true);
        }
    }
}