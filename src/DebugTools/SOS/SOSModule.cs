using System;
using System.Collections.Generic;
using ClrDebug;

namespace DebugTools.SOS
{
    [Serializable]
    public class SOSModule
    {
        public static SOSModule[] GetModules(SOSAssembly assembly, SOSDacInterface sos)
        {
            var list = new List<SOSModule>();

            var moduleAddresses = sos.GetAssemblyModuleList(assembly.AssemblyPtr);

            foreach (var address in moduleAddresses)
            {
                var data = sos.GetModuleData(address);
                list.Add(new SOSModule(address, assembly, data, sos));
            }

            return list.ToArray();
        }

        public static SOSModule GetModule(CLRDATA_ADDRESS address, SOSDacInterface sos)
        {
            if (sos.TryGetModuleData(address, out var data) != HRESULT.S_OK)
                return null;

            var assembly = SOSAssembly.GetAssembly(data.Assembly, sos);

            return new SOSModule(address, assembly, data, sos);
        }

        public string FileName { get; }
        public SOSAssembly Assembly { get; }

        public CLRDATA_ADDRESS Address { get; }
        public CLRDATA_ADDRESS PEAssembly { get; }
        public CLRDATA_ADDRESS ilBase { get; }
        public CLRDATA_ADDRESS metadataStart { get; }
        public long metadataSize { get; }
        public bool bIsReflection { get; }
        public bool bIsPEFile { get; }
        public long dwBaseClassIndex { get; }
        public long dwModuleID { get; }
        public DacpModuleDataTransientFlags dwTransientFlags { get; }
        public CLRDATA_ADDRESS TypeDefToMethodTableMap { get; }
        public CLRDATA_ADDRESS TypeRefToMethodTableMap { get; }
        public CLRDATA_ADDRESS MethodDefToDescMap { get; }
        public CLRDATA_ADDRESS FieldDefToDescMap { get; }
        public CLRDATA_ADDRESS MemberRefToDescMap { get; }
        public CLRDATA_ADDRESS FileReferencesMap { get; }
        public CLRDATA_ADDRESS ManifestModuleReferencesMap { get; }
        public CLRDATA_ADDRESS pLookupTableHeap { get; }
        public CLRDATA_ADDRESS pThunkHeap { get; }
        public long dwModuleIndex { get; }

        private SOSModule(CORDB_ADDRESS address, SOSAssembly assembly, DacpModuleData data, SOSDacInterface sos)
        {
            Assembly = assembly;
            Address = data.Address;
            PEAssembly = data.PEAssembly;
            ilBase = data.ilBase;
            metadataStart = data.metadataStart;
            metadataSize = data.metadataSize;
            bIsReflection = data.bIsReflection;
            bIsPEFile = data.bIsPEFile;
            dwBaseClassIndex = data.dwBaseClassIndex;
            dwModuleID = data.dwModuleID;
            dwTransientFlags = data.dwTransientFlags;
            TypeDefToMethodTableMap = data.TypeDefToMethodTableMap;
            TypeRefToMethodTableMap = data.TypeRefToMethodTableMap;
            MethodDefToDescMap = data.MethodDefToDescMap;
            FieldDefToDescMap = data.FieldDefToDescMap;
            MemberRefToDescMap = data.MemberRefToDescMap;
            FileReferencesMap = data.FileReferencesMap;
            ManifestModuleReferencesMap = data.ManifestModuleReferencesMap;
            pLookupTableHeap = data.pLookupTableHeap;
            pThunkHeap = data.pThunkHeap;
            dwModuleIndex = data.dwModuleIndex;

            var module = sos.GetModule(address);

            if (assembly.isDynamic)
                FileName = assembly.Name;
            else
            {
                if (module.TryGetFileName(out var name) == HRESULT.S_OK)
                    FileName = name;
            }
        }

        public override string ToString()
        {
            return FileName;
        }
    }
}
