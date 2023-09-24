using System.Diagnostics;

namespace DebugTools
{
    class SOSLocalDbgSessionProvider : LocalDbgSessionProvider<LocalSOSProcess>
    {
        public SOSLocalDbgSessionProvider() : base(DbgSessionType.SOS, "Process")
        {
        }

        protected override LocalSOSProcess CreateSubSessionInternal(Process process, bool debugTarget)
        {
            var hostApp = Store.GetDetectedHost(process, debugTarget);

            var handle = hostApp.CreateSOSProcess(process.Id, false);

            return new LocalSOSProcess(handle, hostApp);
        }

        protected override bool IsAlive(LocalSOSProcess subSession) => !subSession.Process.HasExited;
    }
}
