using System;
using System.Collections.Generic;
using ClrDebug;

namespace DebugTools.SOS
{
    [Serializable]
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

        public SOSMethodTable ParentMethodTable { get; }

        /// <summary>
        /// Indicates if the runtime has native code available for the given instantiation of the method.
        /// </summary>
        public bool bHasNativeCode { get; }

        /// <summary>
        /// Indicates if the method is generated dynamically through lightweight code generation.
        /// </summary>
        public bool bIsDynamic { get; }

        /// <summary>
        /// The method's slot number in the method table.
        /// </summary>
        public ushort wSlotNumber { get; }

        /// <summary>
        /// The method's initial native address.
        /// </summary>
        public CLRDATA_ADDRESS NativeCodeAddr { get; }

        public CLRDATA_ADDRESS AddressOfNativeCodeSlot { get; }

        /// <summary>
        /// Pointer to the MethodDesc in the runtime.
        /// </summary>
        public CLRDATA_ADDRESS MethodDescPtr { get; }

        public CLRDATA_ADDRESS MethodTablePtr { get; }

        public CLRDATA_ADDRESS ModulePtr { get; }

        /// <summary>
        /// Token associated with the given method.
        /// </summary>
        public mdToken MDToken { get; }

        public CLRDATA_ADDRESS GCInfo { get; }

        public CLRDATA_ADDRESS GCStressCodeCopy { get; }

        /// <summary>
        /// If the method is dynamic, the runtime uses this buffer internally for information tracking.
        /// </summary>
        public CLRDATA_ADDRESS managedDynamicMethodObject { get; }

        /// <summary>
        /// Used to populate the structure per request when given a native code address.
        /// </summary>
        public CLRDATA_ADDRESS requestedIP { get; }

        /// <summary>
        /// Number of times the method has been rejitted through instrumentation.
        /// </summary>
        public int cJittedRejitVersions { get; }

        private SOSMethodDesc(SOSMethodTable methodTable, string name, DacpMethodDescData data)
        {
            Name = name;
            ParentMethodTable = methodTable;

            bHasNativeCode = data.bHasNativeCode;
            bIsDynamic = data.bIsDynamic;
            wSlotNumber = data.wSlotNumber;
            NativeCodeAddr = data.NativeCodeAddr;
            AddressOfNativeCodeSlot = data.AddressOfNativeCodeSlot;
            MethodDescPtr = data.MethodDescPtr;
            MethodTablePtr = data.MethodTablePtr;
            ModulePtr = data.ModulePtr;
            MDToken = data.MDToken;
            GCInfo = data.GCInfo;
            GCStressCodeCopy = data.GCStressCodeCopy;
            managedDynamicMethodObject = data.managedDynamicMethodObject;
            requestedIP = data.requestedIP;
            cJittedRejitVersions = data.cJittedRejitVersions;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
