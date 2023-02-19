using System.Management.Automation;
using ClrDebug;
using DebugTools.SOS;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "SOSModule", DefaultParameterSetName = ParameterSet.Address)]
    public class GetSOSModule : SOSCmdlet
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = ParameterSet.Default)]
        public SOSAssembly Assembly { get; set; }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = ParameterSet.Address)]
        public CLRDATA_ADDRESS Address { get; set; }

        protected override void ProcessRecordEx()
        {
            if (ParameterSetName == ParameterSet.Address)
            {
                var assembly = HostApp.GetSOSModule(Process, Address);

                if (assembly == null)
                    WriteWarning($"{Address} is not a valid Module");
                else
                    WriteObject(assembly);
            }
            else
            {
                var modules = HostApp.GetSOSModules(Process, Assembly);

                foreach (var module in modules)
                    WriteObject(module);
            }
        }
    }
}
