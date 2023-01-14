using System.Collections.Generic;
using System.Management.Automation;
using ClrDebug;
using DebugTools.SOS;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "SOSFieldDesc", DefaultParameterSetName = ParameterSet.Address)]
    public class GetSOSFieldDesc : SOSCmdlet
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = ParameterSet.Default)]
        public SOSMethodTable MethodTable { get; set; }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = ParameterSet.Address)]
        public CLRDATA_ADDRESS Address { get; set; }

        protected override void ProcessRecordEx()
        {
            if (ParameterSetName == ParameterSet.Address)
            {
                var assembly = SOSFieldDesc.GetFieldDesc(Address, SOS);

                if (assembly == null)
                    WriteWarning($"{Address} is not a valid FieldDesc");
                else
                    WriteObject(assembly);
            }
            else
            {
                IEnumerable<SOSFieldDesc> fieldDescs = SOSFieldDesc.GetFieldDescs(MethodTable, SOS);

                foreach (var fieldDesc in fieldDescs)
                    WriteObject(fieldDesc);
            }
        }
    }
}
