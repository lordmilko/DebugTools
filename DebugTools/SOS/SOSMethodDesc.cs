using System.Collections.Generic;
using ClrDebug;

namespace DebugTools.SOS
{
    public class SOSMethodDesc
    {
        public static SOSMethodDesc[] GetMethodDescs(SOSMethodTable methodTable, SOSDacInterface sos)
        {
            var list = new List<SOSMethodDesc>();

            for (var i = 0; i < methodTable.wNumMethods; i++)
            {
                if (sos.TryGetMethodTableSlot(methodTable.Address, i, out var ipAddress) != HRESULT.S_OK)
                    continue;

                if (sos.TryGetCodeHeaderData(ipAddress, out var codeHeaderData) != HRESULT.S_OK)
                    continue;

                var data = new DacpMethodDescData();

                if (data.Request(sos.Raw, codeHeaderData.MethodDescPtr) != HRESULT.S_OK)
                    continue;

                var name = sos.GetMethodDescName(codeHeaderData.MethodDescPtr);

                list.Add(new SOSMethodDesc(methodTable, name, data));
            }

            return list.ToArray();
        }

        public static SOSMethodDesc GetMethodDesc(CLRDATA_ADDRESS address, SOSDacInterface sos)
        {
            var data = new DacpMethodDescData();

            if (data.Request(sos.Raw, address) != HRESULT.S_OK)
                return null;

            var name = sos.GetMethodDescName(address);

            var methodTable = SOSMethodTable.GetMethodTable(data.MethodTablePtr, sos);

            return new SOSMethodDesc(methodTable, name, data);
        }

        public string Name { get; }

        /// <summary>
        /// Indicates if the runtime has native code available for the given instantiation of the method.
        /// </summary>
        public bool bHasNativeCode => data.bHasNativeCode;

        /// <summary>
        /// Indicates if the method is generated dynamically through lightweight code generation.
        /// </summary>
        public bool bIsDynamic => data.bIsDynamic;

        /// <summary>
        /// The method's slot number in the method table.
        /// </summary>
        public ushort wSlotNumber => data.wSlotNumber;

        /// <summary>
        /// The method's initial native address.
        /// </summary>
        public CLRDATA_ADDRESS NativeCodeAddr => data.NativeCodeAddr;

        public CLRDATA_ADDRESS AddressOfNativeCodeSlot => data.AddressOfNativeCodeSlot;

        /// <summary>
        /// Pointer to the MethodDesc in the runtime.
        /// </summary>
        public CLRDATA_ADDRESS MethodDescPtr => data.MethodDescPtr;

        public CLRDATA_ADDRESS MethodTablePtr => data.MethodTablePtr;

        public CLRDATA_ADDRESS ModulePtr => data.ModulePtr;

        /// <summary>
        /// Token associated with the given method.
        /// </summary>
        public mdToken MDToken => data.MDToken;

        public CLRDATA_ADDRESS GCInfo => data.GCInfo;

        public CLRDATA_ADDRESS GCStressCodeCopy => data.GCStressCodeCopy;

        /// <summary>
        /// If the method is dynamic, the runtime uses this buffer internally for information tracking.
        /// </summary>
        public CLRDATA_ADDRESS managedDynamicMethodObject => data.managedDynamicMethodObject;

        /// <summary>
        /// Used to populate the structure per request when given a native code address.
        /// </summary>
        public CLRDATA_ADDRESS requestedIP => data.requestedIP;

        /// <summary>
        /// Information about the latest instrumented version of the method.
        /// </summary>
        public DacpReJitData rejitDataCurrent => data.rejitDataCurrent;

        /// <summary>
        /// Rejit information for the requested native address.
        /// </summary>
        public DacpReJitData rejitDataRequested => data.rejitDataRequested;

        /// <summary>
        /// Number of times the method has been rejitted through instrumentation.
        /// </summary>
        public int cJittedRejitVersions => data.cJittedRejitVersions;

        private DacpMethodDescData data;

        private SOSMethodDesc(SOSMethodTable methodTable, string name, DacpMethodDescData data)
        {
            Name = name;
            this.data = data;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
