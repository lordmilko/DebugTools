using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using ClrDebug;
using DebugTools.SOS;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "SOSAppDomain", DefaultParameterSetName = ParameterSet.Default)]
    public class GetSOSAppDomain : SOSCmdlet
    {
        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Default)]
        public AppDomainType[] Type { get; set; }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = ParameterSet.Address)]
        public CLRDATA_ADDRESS Address { get; set; }

        protected override void ProcessRecordEx()
        {
            if (ParameterSetName == ParameterSet.Address)
            {
                var assembly = HostApp.GetSOSAppDomain(Process, Address);

                if (assembly == null)
                    WriteWarning($"{Address} is not a valid AppDomain");
                else
                    WriteObject(assembly);
            }
            else
            {
                IEnumerable<SOSAppDomain> domains = HostApp.GetSOSAppDomains(Process);

                if (Type != null)
                    domains = domains.Where(d => Type.Contains(d.Type));

                foreach (var domain in domains)
                    WriteObject(domain);
            }
        }
    }
}
