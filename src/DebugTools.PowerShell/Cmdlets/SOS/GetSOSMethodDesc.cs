using System.Collections.Generic;
using System.Management.Automation;
using ClrDebug;
using DebugTools.SOS;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "SOSMethodDesc", DefaultParameterSetName = ParameterSet.Address)]
    public class GetSOSMethodDesc : SOSCmdlet
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = ParameterSet.Default)]
        public SOSMethodTable MethodTable { get; set; }

        [Parameter(Mandatory = false, Position = 0, ParameterSetName = ParameterSet.Default)]
        public string[] Name { get; set; }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = ParameterSet.Address)]
        public CLRDATA_ADDRESS Address { get; set; }

        protected override void ProcessRecordEx()
        {
            if (ParameterSetName == ParameterSet.Address)
            {
                var assembly = HostApp.GetSOSMethodDesc(Process, Address);

                if (assembly == null)
                    WriteWarning($"{Address} is not a valid MethodDesc");
                else
                    WriteObject(assembly);
            }
            else
            {
                IEnumerable<SOSMethodDesc> methodDescs = HostApp.GetSOSMethodDescs(Process, MethodTable);

                if (Name != null)
                    methodDescs = methodDescs.FilterBy(a => a.Name, Name);

                foreach (var methodDesc in methodDescs)
                    WriteObject(methodDesc);
            }
        }
    }
}
