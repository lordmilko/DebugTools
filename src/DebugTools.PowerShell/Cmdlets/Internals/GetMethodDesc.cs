using System.Management.Automation;
using System.Reflection;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "MethodDesc")]
    public class GetMethodDesc : PSCmdlet
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0)]
        public MethodInfo MethodInfo { get; set; }

        protected override unsafe void ProcessRecord()
        {
            var methodHandle = MethodInfo.MethodHandle.Value;

            var methodDesc = *(MethodDesc*) methodHandle;

            WriteObject(methodDesc);
        }
    }
}