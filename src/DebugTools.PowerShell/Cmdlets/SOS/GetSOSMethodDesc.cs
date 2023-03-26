﻿using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using ClrDebug;
using DebugTools.SOS;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "SOSMethodDesc", DefaultParameterSetName = ParameterSet.Default)]
    public class GetSOSMethodDesc : SOSCmdlet
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = ParameterSet.AppDomain)]
        public SOSAppDomain AppDomain { get; set; }

        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = ParameterSet.Assembly)]
        public SOSAssembly Assembly { get; set; }

        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = ParameterSet.Module)]
        public SOSModule Module { get; set; }

        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = ParameterSet.MethodTable)]
        public SOSMethodTable MethodTable { get; set; }

        [Parameter(Mandatory = false, Position = 0, ParameterSetName = ParameterSet.Default)]
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = ParameterSet.AppDomain)]
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = ParameterSet.Assembly)]
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = ParameterSet.Module)]
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = ParameterSet.MethodTable)]
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

                case ParameterSet.MethodTable:
                    ProcessMethodTable(MethodTable);
                    break;

                case ParameterSet.Address:
                    ProcessAddress();
                    break;

                default:
                    throw new UnknownParameterSetException(ParameterSetName);
            }
        }

        protected override void ProcessMethodTable(SOSMethodTable methodTable)
        {
            IEnumerable<SOSMethodDesc> methodDescs = HostApp.GetSOSMethodDescs(Process, methodTable);

            if (NameOrAddress != null)
                methodDescs = methodDescs.FilterBy(a => a.Name, NameOrAddress.Select(v => v.Left).ToArray());

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

        private void ProcessAddress()
        {
            foreach (var item in NameOrAddress)
            {
                var address = item.Right;

                var methodDesc = HostApp.GetSOSMethodDesc(Process, address);

                if (methodDesc == null)
                    WriteWarning($"{address} is not a valid MethodDesc");
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
        }

        private void WriteRawMethodDesc(MethodDesc raw, SOSMethodDesc sos)
        {
            var pso = new PSObject(raw);
            pso.Properties.Add(new PSNoteProperty("Address", sos.MethodDescPtr));
            WriteObject(pso);
        }
    }
}
