using System;
using System.Linq;
using System.Management.Automation;
using DebugTools.Profiler;

namespace DebugTools.PowerShell.Cmdlets
{
    public abstract class ProfilerSessionCmdlet : ProfilerCmdlet
    {
        //Can't pipe because StackFrameCmdlet will be super slow binding trying to figure out which parameter to bind to
        [Parameter(Mandatory = false)]
        public ProfilerSession Session { get; set; }

        protected sealed override void ProcessRecord()
        {
            if (Session == null)
                Session = DebugToolsSessionState.GetImplicitProfilerSession();

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
