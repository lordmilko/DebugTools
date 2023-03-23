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

        [Parameter(Mandatory = false)]
        public SwitchParameter Raw { get; set; }

        protected override void ProcessRecordEx()
        {
            if (ParameterSetName == ParameterSet.Address)
            {
                var methodDesc = HostApp.GetSOSMethodDesc(Process, Address);

                if (methodDesc == null)
                    WriteWarning($"{Address} is not a valid MethodDesc");
                else
                {
                    if (Raw)
                    {
                        var raw = HostApp.GetRawMethodDesc(Process, methodDesc.MethodDescPtr);

                        WriteRawMethodDesc(raw, methodDesc);
                    }
                    else
                        WriteObject(methodDesc);
                }
            }
            else
            {
                IEnumerable<SOSMethodDesc> methodDescs = HostApp.GetSOSMethodDescs(Process, MethodTable);

                if (Name != null)
                    methodDescs = methodDescs.FilterBy(a => a.Name, Name);

                foreach (var methodDesc in methodDescs)
                {
                    if (Raw)
                    {
                        var raw = HostApp.GetRawMethodDesc(Process, methodDesc.MethodDescPtr);

                        WriteRawMethodDesc(raw, methodDesc);
                    }
                    else
                        WriteObject(methodDesc);
                }
            }
        }

        private void WriteRawMethodDesc(MethodDesc raw, SOSMethodDesc sos)
        {
            var pso = new PSObject(raw);
            pso.Properties.Add(new PSNoteProperty("Address", sos.MethodDescPtr));
            WriteObject(pso);
        }
    }
}
