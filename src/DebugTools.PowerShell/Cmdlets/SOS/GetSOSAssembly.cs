using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using ClrDebug;
using DebugTools.SOS;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "SOSAssembly", DefaultParameterSetName = ParameterSet.Default)]
    public class GetSOSAssembly : SOSCmdlet
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = ParameterSet.AppDomain)]
        public SOSAppDomain AppDomain { get; set; }

        [Alias("Name", "Address")]
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = ParameterSet.Default)]
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = ParameterSet.AppDomain)]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = ParameterSet.Address)]
        public Either<string, CLRDATA_ADDRESS>[] NameOrAddress
        {
            get => nameOrAddress;
            set => nameOrAddress = value;
        }

        protected override void ProcessRecordEx()
        {
            switch (ParameterSetName)
            {
                case ParameterSet.Default:
                    ProcessDefault();
                    break;

                case ParameterSet.AppDomain:
                    ProcessAppDomain(AppDomain);
                    break;

                case ParameterSet.Address:
                    ProcessAddress();
                    break;

                default:
                    throw new UnknownParameterSetException(ParameterSetName);
            }
        }

        protected override void ProcessAppDomain(SOSAppDomain appDomain)
        {
            IEnumerable<SOSAssembly> assemblies = HostApp.GetSOSAssemblies(Process, appDomain);

            if (NameOrAddress != null)
                assemblies = assemblies.FilterBy(a => a.Name, NameOrAddress.Select(v => v.Left).ToArray());

            foreach (var assembly in assemblies)
                WriteObject(assembly);
        }

        private void ProcessAddress()
        {
            foreach (var item in NameOrAddress)
            {
                var address = item.Right;

                var assembly = HostApp.GetSOSAssembly(Process, address);

                if (assembly == null)
                    WriteWarning($"{address} is not a valid Assembly");
                else
                    WriteObject(assembly);
            }
        }
    }
}
