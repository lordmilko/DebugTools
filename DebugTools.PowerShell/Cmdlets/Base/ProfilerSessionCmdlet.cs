using System;
using System.Linq;
using System.Management.Automation;

namespace DebugTools.PowerShell.Cmdlets
{
    public abstract class ProfilerSessionCmdlet : PSCmdlet
    {
        [Parameter(Mandatory = false, ValueFromPipeline = true)]
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
                    throw new InvalidOperationException($"Cannot execute cmdlet: no -{nameof(Session)} was specified and no global {nameof(Session)} could be found in the PowerShell session.");
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