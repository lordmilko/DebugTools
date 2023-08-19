using System.Collections.Generic;
using System.Diagnostics;
using DebugTools.Host;

namespace DebugTools
{
    class HostInfo
    {
        /// <summary>
        /// Gets the host.
        /// </summary>
        public HostApp Host { get; }

        /// <summary>
        /// Gets the process that the host is contained in.
        /// </summary>
        public Process Process { get; }

        //A list of all processes currently utilizing the host
        private HashSet<int> users = new HashSet<int>();

        public HostInfo(HostApp host, Process process)
        {
            Host = host;
            Process = process;
        }

        public void AddUser(Process process)
        {
            users.Add(process.Id);
        }

        public bool Release(int processId)
        {
            users.Remove(processId);

            if (users.Count == 0)
            {
                if (Process.GetCurrentProcess().Id != Process.Id)
                    Process.Kill();

                return true;
            }

            return false;
        }
    }
}
