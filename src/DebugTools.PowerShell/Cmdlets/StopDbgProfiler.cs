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
                session = DebugToolsSessionState.Services.GetOrCreateSpecial<ProfilerSession>(new CreateSpecialProfilerContext(ProfilerSessionType.Global, false, false));
            else
            {
                if (session == null)
                    session = DebugToolsSessionState.Services.GetImplicitService<ProfilerSession>(false);
            }

            if (session != null)
            {
                session.Status = ProfilerSessionStatus.Terminated;

                DebugToolsSessionState.Services.Close(
                    session.PID ?? -1,
                    session
                );

                GC.Collect();
            }
        }
    }
}
