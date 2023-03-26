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
            switch (ParameterSetName)
            {
                case ParameterSet.Default:
                    ProcessDefault();
                    break;

                case ParameterSet.Address:
                    ProcessAddress();
                    break;

                default:
                    throw new UnknownParameterSetException(ParameterSetName);
            }
        }

        protected override void ProcessDefault()
        {
            IEnumerable<SOSAppDomain> domains = HostApp.GetSOSAppDomains(Process);

            if (Type != null)
                domains = domains.Where(d => Type.Contains(d.Type));

            foreach (var domain in domains)
                WriteObject(domain);
        }

        private void ProcessAddress()
        {
            var appDomain = HostApp.GetSOSAppDomain(Process, Address);

            if (appDomain == null)
                WriteWarning($"{Address} is not a valid AppDomain");
            else
                WriteObject(appDomain);
        }
    }
}
