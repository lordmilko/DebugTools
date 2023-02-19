using System;
using System.Collections.Generic;
using ClrDebug;

namespace DebugTools.SOS
{
    public enum AppDomainType
    {
        Shared,
        System,
        Normal
    }

    [Serializable]
    public class SOSAppDomain
    {
        public static SOSAppDomain[] GetAppDomains(SOSDacInterface sos)
        {
            var list = new List<SOSAppDomain>();

            var appDomainStoreData = sos.AppDomainStoreData;

            if (appDomainStoreData.sharedDomain != 0)
                list.Add(new SOSAppDomain(appDomainStoreData.sharedDomain, sos, AppDomainType.Shared));

            //System Domain should always exist, however if you query too quickly after the process has started it may not exist yet
            if (appDomainStoreData.systemDomain != 0)
                list.Add(new SOSAppDomain(appDomainStoreData.systemDomain, sos, AppDomainType.System));

            var appDomainAddresses = sos.GetAppDomainList(appDomainStoreData.DomainCount);

            foreach (var domain in appDomainAddresses)
            {
                list.Add(new SOSAppDomain(domain, sos, AppDomainType.Normal));
            }

            return list.ToArray();
        }

        public static SOSAppDomain GetAppDomain(CLRDATA_ADDRESS address, SOSDacInterface sos)
        {
            if (sos.TryGetAppDomainData(address, out var data) != HRESULT.S_OK)
                return null;

            var appDomainStoreData = sos.AppDomainStoreData;

            if (appDomainStoreData.sharedDomain == address)
                return new SOSAppDomain(address, data, sos, AppDomainType.Shared);

            if (appDomainStoreData.systemDomain == address)
                return new SOSAppDomain(address, data, sos, AppDomainType.System);

            return new SOSAppDomain(address, data, sos, AppDomainType.Normal);
        }

        public string Name { get; }

        public AppDomainType Type { get; }

        public CLRDATA_ADDRESS AppDomainPtr { get; }
        public CLRDATA_ADDRESS AppSecDesc { get; }
        public CLRDATA_ADDRESS pLowFrequencyHeap { get; }
        public CLRDATA_ADDRESS pHighFrequencyHeap { get; }
        public CLRDATA_ADDRESS pStubHeap { get; }
        public CLRDATA_ADDRESS DomainLocalBlock { get; }
        public CLRDATA_ADDRESS pDomainLocalModules { get; }
        public int dwId { get; }
        public int AssemblyCount { get; }
        public int FailedAssemblyCount { get; }
        public DacpAppDomainDataStage AppDomainStage { get; }

        private SOSAppDomain(CLRDATA_ADDRESS address, SOSDacInterface sos, AppDomainType type) : this(address, sos.GetAppDomainData(address), sos, type)
        {
        }

        private SOSAppDomain(CLRDATA_ADDRESS address, DacpAppDomainData data, SOSDacInterface sos, AppDomainType type)
        {
            Type = type;
            AppDomainPtr = data.AppDomainPtr;
            AppSecDesc = data.AppSecDesc;
            pLowFrequencyHeap = data.pLowFrequencyHeap;
            pHighFrequencyHeap = data.pHighFrequencyHeap;
            pStubHeap = data.pStubHeap;
            DomainLocalBlock = data.DomainLocalBlock;
            pDomainLocalModules = data.pDomainLocalModules;
            dwId = data.dwId;
            AssemblyCount = data.AssemblyCount;
            FailedAssemblyCount = data.FailedAssemblyCount;
            AppDomainStage = data.AppDomainStage;

            switch (type)
            {
                case AppDomainType.Normal:
                    Name = sos.GetAppDomainName(address);
                    break;

                case AppDomainType.Shared:
                    Name = "Shared";
                    break;

                case AppDomainType.System:
                    Name = "System";
                    break;

                default:
                    throw new NotImplementedException($"Don't know how to handle {nameof(AppDomain)} '{type}'.");
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
