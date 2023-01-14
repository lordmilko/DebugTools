using System.Collections.Generic;
using System.Management.Automation;
using ClrDebug;
using DebugTools.SOS;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "SOSAssembly", DefaultParameterSetName = ParameterSet.Address)]
    public class GetSOSAssembly : SOSCmdlet
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = ParameterSet.Default)]
        public SOSAppDomain AppDomain { get; set; }

        [Parameter(Mandatory = false, Position = 0, ParameterSetName = ParameterSet.Default)]
        public string[] Name { get; set; }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = ParameterSet.Address)]
        public CLRDATA_ADDRESS Address { get; set; }

        protected override void ProcessRecordEx()
        {
            if (ParameterSetName == ParameterSet.Address)
            {
                var assembly = SOSAssembly.GetAssembly(Address, SOS);

                if (assembly == null)
                    WriteWarning($"{Address} is not a valid Assembly");
                else
                    WriteObject(assembly);
            }
            else
            {
                IEnumerable<SOSAssembly> assemblies = SOSAssembly.GetAssemblies(AppDomain, SOS);

                if (Name != null)
                    assemblies = assemblies.FilterBy(a => a.Name, Name);

                foreach (var assembly in assemblies)
                    WriteObject(assembly);
            }
        }
    }
}
