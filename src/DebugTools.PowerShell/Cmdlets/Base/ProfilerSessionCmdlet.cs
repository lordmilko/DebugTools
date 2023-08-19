using System;
using System.Management.Automation;
using DebugTools.Profiler;

namespace DebugTools.PowerShell.Cmdlets
{
    public abstract class ProfilerSessionCmdlet : ProfilerCmdlet
    {
        //Can't pipe because StackFrameCmdlet will be super slow binding trying to figure out which parameter to bind to
        [Parameter(Mandatory = false)]
        public ProfilerSession Session { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Global { get; set; }

        private bool mayCreateGlobalSession;

        protected ProfilerSessionCmdlet(bool mayCreateGlobalSession = false)
        {
            this.mayCreateGlobalSession = mayCreateGlobalSession;
        }

        protected sealed override void ProcessRecord()
        {
            if (Session == null)
            {
                if (Global)
                    Session = DebugToolsSessionState.Services.GetOrCreateSpecial<ProfilerSession>(new CreateSpecialProfilerContext(ProfilerSessionType.Global, true, mayCreateGlobalSession));
                else
                    Session = DebugToolsSessionState.Services.GetImplicitService<ProfilerSession>();
            }

            ProcessRecordEx();
        }

        internal void WriteInvalidOperation(Exception ex, object targetObject = null, ErrorCategory errorCategory = ErrorCategory.InvalidOperation)
        {
            WriteError(new ErrorRecord(
                ex,
                ex.GetType().Name,
                errorCategory,
                targetObject
            ));
        }

        protected abstract void ProcessRecordEx();
    }
}
