using System.Diagnostics;

namespace DebugTools
{
    class SOSLocalDbgSessionProvider : LocalDbgSessionProvider<LocalSOSProcess>
    {
        public SOSLocalDbgSessionProvider() : base(DbgServiceType.SOS, "Process")
        {
        }

        protected override LocalSOSProcess CreateServiceInternal(Process process, bool debugTarget)
        {
            var hostApp = Store.GetDetectedHost(process, debugTarget);

            var handle = hostApp.CreateSOSProcess(process.Id, false);

            return new LocalSOSProcess(handle, hostApp);
        }

        protected override bool IsAlive(LocalSOSProcess service) => !service.Process.HasExited;
    }
}
