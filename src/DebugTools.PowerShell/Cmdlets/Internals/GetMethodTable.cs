using System.Management.Automation;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "MethodTable")]
    public class GetMethodTable : PSCmdlet
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0)]
        public object Object { get; set; }

        protected override unsafe void ProcessRecord()
        {
            var obj = Unwrap(Object);

            var typeHandle = obj.GetType().TypeHandle.Value;

            var methodTable = *(MethodTable*) typeHandle;

            WriteObject(methodTable);
        }

        private object Unwrap(object obj)
        {
            while (obj is PSObject pso)
                obj = pso.BaseObject;

            return obj;
        }
    }
}
