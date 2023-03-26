using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using ClrDebug;
using DebugTools.SOS;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "SOSModule", DefaultParameterSetName = ParameterSet.Default)]
    public class GetSOSModule : SOSCmdlet
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = ParameterSet.AppDomain)]
        public SOSAppDomain AppDomain { get; set; }

        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = ParameterSet.Assembly)]
        public SOSAssembly Assembly { get; set; }

        [Parameter(Mandatory = false, Position = 0, ParameterSetName = ParameterSet.Default)]
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = ParameterSet.AppDomain)]
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = ParameterSet.Assembly)]
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

                case ParameterSet.Assembly:
                    ProcessAssembly(Assembly);
                    break;

                case ParameterSet.Address:
                    ProcessAddress();
                    break;

                default:
                    throw new UnknownParameterSetException(ParameterSetName);
            }
        }

        protected override void ProcessAssembly(SOSAssembly assembly)
        {
            IEnumerable<SOSModule> modules = HostApp.GetSOSModules(Process, assembly);

            if (NameOrAddress != null)
                modules = modules.FilterBy(a => a.FileName, NameOrAddress.Select(v => v.Left).ToArray());

            foreach (var module in modules)
                WriteObject(module);
        }

        private void ProcessAddress()
        {
            foreach (var item in NameOrAddress)
            {
                var address = item.Right;

                var module = HostApp.GetSOSModule(Process, address);

                if (module == null)
                    WriteWarning($"{address} is not a valid Module");
                else
                    WriteObject(module);
            }
        }
    }
}
