using System;
using System.Collections.Generic;
using System.Management.Automation;
using ClrDebug;
using DebugTools.SOS;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "SOSStackTrace", DefaultParameterSetName = ParameterSet.Manual)]
    public class GetSOSStackTrace : SOSCmdlet
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = ParameterSet.Default)]
        public SOSThreadInfo Thread { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = ParameterSet.Manual, Position = 0)]
        public int ThreadId { get; set; }

        protected override void ProcessRecordEx()
        {
            var threadId = GetThreadId();

            if (threadId == 0)
                return;

            using (new SuspendedThread(threadId))
            {
                var result = HostApp.GetSOSStackTrace(Process, threadId);

                if (result is SOSStackFrame[] sosFrames)
                {
                    foreach (var frame in sosFrames)
                        WriteObject(frame);
                }
                else
                    WriteWarning(result.ToString());
            }
        }

        

        private int GetThreadId()
        {
            switch (ParameterSetName)
            {
                case ParameterSet.Default:
                    return Thread.ThreadId;

                case ParameterSet.Manual:
                    return ThreadId;

                default:
                    throw new NotImplementedException($"Don't know how to handle parameter set '{ParameterSetName}'.");
            }
        }
    }
}
