using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using ClrDebug;
using DebugTools.SOS;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "SOSMethodTable", DefaultParameterSetName = ParameterSet.Default)]
    public class GetSOSMethodTable : SOSCmdlet
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = ParameterSet.AppDomain)]
        public SOSAppDomain AppDomain { get; set; }

        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = ParameterSet.Assembly)]
        public SOSAssembly Assembly { get; set; }

        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = ParameterSet.Module)]
        public SOSModule Module { get; set; }

        [Parameter(Mandatory = false, Position = 0, ParameterSetName = ParameterSet.Default)]
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = ParameterSet.AppDomain)]
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = ParameterSet.Assembly)]
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = ParameterSet.Module)]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = ParameterSet.Address)]
        public Either<string, CLRDATA_ADDRESS>[] NameOrAddress
        {
            get => nameOrAddress;
            set => nameOrAddress = value;
        }

        [Parameter(Mandatory = false)]
        public SwitchParameter Raw { get; set; }

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

                case ParameterSet.Module:
                    ProcessModule(Module);
                    break;

                case ParameterSet.Address:
                    ProcessAddress();
                    break;

                default:
                    throw new UnknownParameterSetException(ParameterSetName);
            }
        }

        protected override void ProcessModule(SOSModule module)
        {
            IEnumerable<SOSMethodTable> methodTables = HostApp.GetSOSMethodTables(Process, module);

            if (NameOrAddress != null)
                methodTables = methodTables.FilterBy(a => a.Name, NameOrAddress.Select(v => v.Left).ToArray());

            foreach (var methodTable in methodTables)
            {
                if (Raw)
                {
                    var raw = HostApp.GetRawMethodTable(Process, methodTable.Address);

                    WriteObject(raw);
                }
                else
                {
                    WriteObject(methodTable);
                }
            }
        }

        private void ProcessAddress()
        {
            foreach (var item in NameOrAddress)
            {
                var address = item.Right;

                var methodTable = HostApp.GetSOSMethodTable(Process, address);

                if (methodTable == null)
                    WriteWarning($"{address} is not a valid MethodTable");
                else
                {
                    if (Raw)
                    {
                        //Read the SOSMethodTable above first to validate that the specified address is even valid
                        var raw = HostApp.GetRawMethodTable(Process, methodTable.Address);

                        WriteObject(raw);
                    }
                    else
                        WriteObject(methodTable);
                }
            }
        }
    }
}
