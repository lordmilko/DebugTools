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

    public class SOSAppDomain
    {
        public static SOSAppDomain[] GetAppDomains(SOSDacInterface sos)
        {
            var list = new List<SOSAppDomain>();

            var appDomainStoreData = sos.AppDomainStoreData;

            list.Add(new SOSAppDomain(appDomainStoreData.sharedDomain, sos, AppDomainType.Shared));
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

        public CLRDATA_ADDRESS AppDomainPtr => data.AppDomainPtr;
        public CLRDATA_ADDRESS AppSecDesc => data.AppSecDesc;
        public CLRDATA_ADDRESS pLowFrequencyHeap => data.pLowFrequencyHeap;
        public CLRDATA_ADDRESS pHighFrequencyHeap => data.pHighFrequencyHeap;
        public CLRDATA_ADDRESS pStubHeap => data.pStubHeap;
        public CLRDATA_ADDRESS DomainLocalBlock => data.DomainLocalBlock;
        public CLRDATA_ADDRESS pDomainLocalModules => data.pDomainLocalModules;
        public int dwId => data.dwId;
        public int AssemblyCount => data.AssemblyCount;
        public int FailedAssemblyCount => data.FailedAssemblyCount;
        public DacpAppDomainDataStage AppDomainStage => data.AppDomainStage;

        private readonly DacpAppDomainData data;

        private SOSAppDomain(CLRDATA_ADDRESS address, SOSDacInterface sos, AppDomainType type) : this(address, sos.GetAppDomainData(address), sos, type)
        {
        }

        private SOSAppDomain(CLRDATA_ADDRESS address, DacpAppDomainData data, SOSDacInterface sos, AppDomainType type)
        {
            Type = type;
            this.data = data;

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
