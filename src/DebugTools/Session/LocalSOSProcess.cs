using System.Diagnostics;
using DebugTools.Host;

namespace DebugTools
{
    public class LocalSOSProcess : IHostAppSession
    {
        public Process Process { get; }

        public HostApp HostApp { get; }

        private DbgSessionHandle handle;

        internal LocalSOSProcess(DbgSessionHandle handle, HostApp hostApp)
        {
            this.handle = handle;
            Process = Process.GetProcessById(handle.ProcessId);
            HostApp = hostApp;
        }

        public static implicit operator DbgSessionHandle(LocalSOSProcess process)
        {
            return process.handle;
        }
    }
}
