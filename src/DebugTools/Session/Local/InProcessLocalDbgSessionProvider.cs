using System;
using System.Diagnostics;
using System.Linq;
using DebugTools.Host;

namespace DebugTools
{
    class InProcessLocalDbgSessionProvider : LocalDbgSessionProvider<InjectedHostSession>
    {
        public InProcessLocalDbgSessionProvider() : base(DbgSessionType.InProcess, "Process")
        {
        }

        protected override InjectedHostSession CreateSubSessionInternal(Process process, bool debugHost) =>
            new InjectedHostSession(process, debugHost);

        protected override bool IsAlive(InjectedHostSession subSession) => !subSession.Process.HasExited;

        internal override bool IsValidFallback(int? pid)
        {
            if (pid == null)
                return false;

            try
            {
                var modules = Process.GetProcessById(pid.Value).Modules;

                var expected = new[]
                {
                    "clr.dll",
                    "coreclr.dll"
                };

                if (modules.Cast<ProcessModule>().Any(m => expected.Any(v => v.Equals(m.ModuleName, StringComparison.OrdinalIgnoreCase))))
                    return true;

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
