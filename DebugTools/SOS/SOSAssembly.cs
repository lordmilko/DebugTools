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

            var assemblyAddresses = sos.GetAssemblyList(appDomain.AppDomainPtr);

            foreach (var address in assemblyAddresses)
            {
                var data = sos.GetAssemblyData(appDomain.AppDomainPtr, address);

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
                Name = sos.GetAssemblyName(data.AssemblyPtr);

            Location = sos.GetAssemblyLocation(data.AssemblyPtr);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
