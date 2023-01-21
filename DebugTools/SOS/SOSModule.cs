using System.Collections.Generic;
using ClrDebug;

namespace DebugTools.SOS
{
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

        public CLRDATA_ADDRESS Address => data.Address;
        public CLRDATA_ADDRESS PEAssembly => data.PEAssembly;
        public CLRDATA_ADDRESS ilBase => data.ilBase;
        public CLRDATA_ADDRESS metadataStart => data.metadataStart;
        public long metadataSize => data.metadataSize;
        public bool bIsReflection => data.bIsReflection;
        public bool bIsPEFile => data.bIsPEFile;
        public long dwBaseClassIndex => data.dwBaseClassIndex;
        public long dwModuleID => data.dwModuleID;
        public DacpModuleDataTransientFlags dwTransientFlags => data.dwTransientFlags;
        public CLRDATA_ADDRESS TypeDefToMethodTableMap => data.TypeDefToMethodTableMap;
        public CLRDATA_ADDRESS TypeRefToMethodTableMap => data.TypeRefToMethodTableMap;
        public CLRDATA_ADDRESS MethodDefToDescMap => data.MethodDefToDescMap;
        public CLRDATA_ADDRESS FieldDefToDescMap => data.FieldDefToDescMap;
        public CLRDATA_ADDRESS MemberRefToDescMap => data.MemberRefToDescMap;
        public CLRDATA_ADDRESS FileReferencesMap => data.FileReferencesMap;
        public CLRDATA_ADDRESS ManifestModuleReferencesMap => data.ManifestModuleReferencesMap;
        public CLRDATA_ADDRESS pLookupTableHeap => data.pLookupTableHeap;
        public CLRDATA_ADDRESS pThunkHeap => data.pThunkHeap;
        public long dwModuleIndex => data.dwModuleIndex;

        private MetaDataImport import;

        internal MetaDataImport GetImport(SOSDacInterface sos)
        {
            if (import == null)
            {
                var module = sos.GetModule(Address);
                import = new MetaDataImport((IMetaDataImport)module.Raw);
            }

            return import;
        }

        private readonly DacpModuleData data;

        private SOSModule(CORDB_ADDRESS address, SOSAssembly assembly, DacpModuleData data, SOSDacInterface sos)
        {
            Assembly = assembly;
            this.data = data;

            var module = sos.GetModule(address);

            if (assembly.isDynamic)
                FileName = assembly.Name;
            else
                FileName = module.FileName;
        }

        public override string ToString()
        {
            return FileName;
        }
    }
}
