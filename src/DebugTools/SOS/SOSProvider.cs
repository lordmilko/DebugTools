using System.Linq;
using ClrDebug;

namespace DebugTools.SOS
{
    public class SOSProvider
    {
        private SOSDacInterface sos;

        public SOSProvider(SOSDacInterface sos)
        {
            this.sos = sos;
        }

        private SOSAppDomain[] appDomains;
        public SOSAppDomain[] AppDomains => appDomains ?? (appDomains = SOSAppDomain.GetAppDomains(sos));

        private SOSAssembly[] assemblies;
        public SOSAssembly[] Assemblies => assemblies ?? (assemblies = AppDomains.SelectMany(a => SOSAssembly.GetAssemblies(a, sos)).ToArray());

        private SOSModule[] modules;
        public SOSModule[] Modules => modules ?? (modules = Assemblies.SelectMany(a => SOSModule.GetModules(a, sos)).ToArray());

        private SOSMethodTable[] methodTables;
        public SOSMethodTable[] MethodTables => methodTables ?? (methodTables = Modules.SelectMany(m => SOSMethodTable.GetMethodTables(m, sos)).ToArray());

        private SOSMethodDesc[] methodDescs;
        public SOSMethodDesc[] MethodDescs => methodDescs ?? (methodDescs = MethodTables.SelectMany(m => SOSMethodDesc.GetMethodDescs(m, sos)).ToArray());

        private SOSFieldDesc[] fieldDescs;
        public SOSFieldDesc[] FieldDescs => fieldDescs ?? (fieldDescs = MethodTables.SelectMany(m => SOSFieldDesc.GetFieldDescs(m, sos)).ToArray());
    }
}
