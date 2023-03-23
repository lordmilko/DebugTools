using System;
using System.Management.Automation;
using ClrDebug;
using DebugTools.SOS;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "SOSMethodTable", DefaultParameterSetName = ParameterSet.Address)]
    public class GetSOSMethodTable : SOSCmdlet
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = ParameterSet.Assembly)]
        public SOSAssembly Assembly { get; set; }

        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = ParameterSet.Module)]
        public SOSModule Module { get; set; }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = ParameterSet.Address)]
        public CLRDATA_ADDRESS Address { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Raw { get; set; }

        protected override void ProcessRecordEx()
        {
            if (ParameterSetName == ParameterSet.Address)
            {
                var methodTable = HostApp.GetSOSMethodTable(Process, Address);

                if (methodTable == null)
                    WriteWarning($"{Address} is not a valid MethodTable");
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
            else
            {
                var modules = GetModules();

                foreach (var module in modules)
                {
                    var methodTables = HostApp.GetSOSMethodTables(Process, module);

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
            }
        }

        private SOSModule[] GetModules()
        {
            switch (ParameterSetName)
            {
                case ParameterSet.Module:
                    return new[] {Module};

                case ParameterSet.Assembly:
                    return HostApp.GetSOSModules(Process, Assembly);

                default:
                    throw new NotImplementedException($"Don't know how to handle parameter set '{ParameterSetName}'.");
            }
        }
    }
}
