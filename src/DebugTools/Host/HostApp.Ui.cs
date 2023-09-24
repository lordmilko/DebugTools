using DebugTools.Ui;

namespace DebugTools.Host
{
    public partial class HostApp
    {
        public DbgSessionHandle CreateUiMessageSession(int processId, bool debugTarget) =>
            sessionFactory.CreateSubSession<UiMessageSession>(processId, debugTarget: debugTarget);

        private UiMessageSession GetUiProcess(int processId) =>
            sessionFactory.GetSubSession<UiMessageSession>(processId);

        public bool TryReadMessage(int processId, out WindowMessage message)
        {
            var session = GetUiProcess(processId);

            if (session.TryReadMessage(out message))
                return true;

            return false;
        }
    }
}
