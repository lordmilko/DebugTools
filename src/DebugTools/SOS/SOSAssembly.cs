using System;
using System.Collections.Generic;
using ClrDebug;

namespace DebugTools.SOS
{
    [Serializable]
    public class SOSAssembly
    {
        public static SOSAssembly[] GetAssemblies(SOSAppDomain appDomain, SOSDacInterface sos)
        {
            //You can't ask for the assemblies of the system AppDomain (you'll get E_INVALIDARG)
            if (appDomain.Type == AppDomainType.System)
                return new SOSAssembly[0];

            var list = new List<SOSAssembly>();

            if (sos.TryGetAssemblyList(appDomain.AppDomainPtr, out var assemblyAddresses) != HRESULT.S_OK)
                return new SOSAssembly[0];

            foreach (var address in assemblyAddresses)
            {
                DacpAssemblyData data;

                //We observed a strange issue wherein when attempting to read assemblies against the Framework Version String Domain which exists in MSTest, we'd get CORDBG_E_READVIRTUAL_FAILURE
                //if we attempted to read this domain's assemblies after already having read the method tables of any of our prior modules. Flushing the DAC shows that in fact the target AppDomain
                //has now been unloaded!
                if (sos.TryGetAssemblyData(appDomain.AppDomainPtr, address, out data) == HRESULT.S_OK)
                    list.Add(new SOSAssembly(address, appDomain, data, sos));
            }

            return list.ToArray();
        }

        public static SOSAssembly GetAssembly(CLRDATA_ADDRESS address, SOSDacInterface sos)
        {
            if (sos.TryGetAssemblyData(0, address, out var data) != HRESULT.S_OK)
                return null;

            var appDomain = SOSAppDomain.GetAppDomain(data.ParentDomain, sos);

            return new SOSAssembly(address, appDomain, data, sos);
        }

        public string Name { get; }
        public SOSAppDomain AppDomain { get; }
        public string Location { get; }

        public CLRDATA_ADDRESS AssemblyPtr { get; }
        public CLRDATA_ADDRESS ClassLoader { get; }
        public CLRDATA_ADDRESS ParentDomain { get; }
        public CLRDATA_ADDRESS BaseDomainPtr { get; }
        public CLRDATA_ADDRESS AssemblySecDesc { get; }
        public bool isDynamic { get; }
        public int ModuleCount { get; }
        public int LoadContext { get; }
        public bool isDomainNeutral { get; }
        public int dwLocationFlags { get; }

        private SOSAssembly(CLRDATA_ADDRESS address, SOSAppDomain appDomain, DacpAssemblyData data, SOSDacInterface sos)
        {
            AppDomain = appDomain;
            AssemblyPtr = data.AssemblyPtr;
            ClassLoader = data.ClassLoader;
            ParentDomain = data.ParentDomain;
            BaseDomainPtr = data.BaseDomainPtr;
            AssemblySecDesc = data.AssemblySecDesc;
            isDynamic = data.isDynamic;
            ModuleCount = data.ModuleCount;
            LoadContext = data.LoadContext;
            isDomainNeutral = data.isDomainNeutral;
            dwLocationFlags = data.dwLocationFlags;

            if (data.isDynamic)
                Name = "Dynamic";
            else
            {
                if (sos.TryGetAssemblyName(data.AssemblyPtr, out var name) == HRESULT.S_OK)
                    Name = name;
            }

            Location = sos.GetAssemblyLocation(data.AssemblyPtr);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
