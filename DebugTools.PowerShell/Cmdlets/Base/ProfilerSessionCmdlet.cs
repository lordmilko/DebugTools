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
            {
                var sessions = ProfilerSessionState.Sessions.Where(i => !i.Process.HasExited).ToArray();

                if (sessions.Length == 1)
                    Session = sessions[0];
                else if (sessions.Length > 1)
                    throw new InvalidOperationException($"Cannot execute cmdlet: more than one running global {nameof(ProfilerSession)} was found. Please specify a session explicitly to the -{nameof(Session)} parameter.");
                else
                {
                    if (ProfilerSessionState.Sessions.Count == 1)
                        Session = ProfilerSessionState.Sessions[0];
                    {
                        if (sessions.Length > 0)
                            throw new InvalidOperationException($"Cannot execute cmdlet: no -{nameof(Session)} was specified and more than one {nameof(Session)} belonging to active processes was found in the PowerShell session.");
                        
                        if (ProfilerSessionState.Sessions.Count > 0)
                            throw new InvalidOperationException($"Cannot execute cmdlet: no -{nameof(Session)} was specified, there are no sessions belonging to active processes, and more than one {nameof(Session)} belonging to terminated processes was found in the PowerShell session.");

                        throw new InvalidOperationException($"Cannot execute cmdlet: no -{nameof(Session)} was specified and no global {nameof(Session)} could be found in the PowerShell session.");
                    }
                    
                }
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
