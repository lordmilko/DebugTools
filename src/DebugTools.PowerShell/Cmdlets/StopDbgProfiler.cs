using DebugTools.Profiler;
using System;
using System.Management.Automation;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Stop, "DbgProfiler")]
    public class StopDbgProfiler : DebugToolsCmdlet
    {
        [Parameter(Mandatory = false)]
        public ProfilerSession Session { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Global { get; set; }

        protected override void ProcessRecord()
        {
            var session = Session;

            if (Global)
                session = DebugToolsSessionState.GlobalProfilerSession;
            else
            {
                if (session == null)
                    session = DebugToolsSessionState.GetImplicitProfilerSession(false);
            }

            if (session != null)
            {
                session.Dispose();
                DebugToolsSessionState.ProfilerSessions.Remove(session);

                if (session == DebugToolsSessionState.GlobalProfilerSession)
                    DebugToolsSessionState.GlobalProfilerSession = null;

                GC.Collect();
            }
        }
    }
}
