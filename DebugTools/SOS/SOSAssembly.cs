using System.Collections.Generic;
using ClrDebug;

namespace DebugTools.SOS
{
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

        public CLRDATA_ADDRESS AssemblyPtr => data.AssemblyPtr;
        public CLRDATA_ADDRESS ClassLoader => data.ClassLoader;
        public CLRDATA_ADDRESS ParentDomain => data.ParentDomain;
        public CLRDATA_ADDRESS BaseDomainPtr => data.BaseDomainPtr;
        public CLRDATA_ADDRESS AssemblySecDesc => data.AssemblySecDesc;
        public bool isDynamic => data.isDynamic;
        public int ModuleCount => data.ModuleCount;
        public int LoadContext => data.LoadContext;
        public bool isDomainNeutral => data.isDomainNeutral;
        public int dwLocationFlags => data.dwLocationFlags;

        private readonly DacpAssemblyData data;

        private SOSAssembly(CLRDATA_ADDRESS address, SOSAppDomain appDomain, DacpAssemblyData data, SOSDacInterface sos)
        {
            AppDomain = appDomain;
            this.data = data;

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
