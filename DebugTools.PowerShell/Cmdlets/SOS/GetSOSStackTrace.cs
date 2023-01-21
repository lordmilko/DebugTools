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

            using (new SuspendedThread(threadId))
            {
                var xclrDataProcess = SOS.As<XCLRDataProcess>();

                var hr = xclrDataProcess.TryGetTaskByOSThreadID(threadId, out var task);

                if (hr != HRESULT.S_OK)
                {
                    WriteWarning($"Failed to get {nameof(XCLRDataTask)} for thread {threadId}: {hr}");
                    return;
                }

                hr = task.TryCreateStackWalk(
                    CLRDataSimpleFrameType.CLRDATA_SIMPFRAME_UNRECOGNIZED |
                    CLRDataSimpleFrameType.CLRDATA_SIMPFRAME_MANAGED_METHOD |
                    CLRDataSimpleFrameType.CLRDATA_SIMPFRAME_RUNTIME_MANAGED_CODE |
                    CLRDataSimpleFrameType.CLRDATA_SIMPFRAME_RUNTIME_UNMANAGED_CODE,
                    out var stackWalk
                );

                if (hr != HRESULT.S_OK)
                {
                    WriteWarning($"Failed to get {nameof(XCLRDataStackWalk)} for thread {threadId}: {hr}");
                    return;
                }

                var sosFrames = new List<SOSStackFrame>();

                do
                {
                    CLRDATA_ADDRESS ip;
                    CLRDATA_ADDRESS sp;
                    hr = GetFrameLocation(stackWalk, out ip, out sp);

                    if (hr == HRESULT.S_OK)
                    {
                        DacpFrameData frameData = new DacpFrameData();
                        var frameDataResult = frameData.Request(stackWalk.Raw);

                        if (frameDataResult == HRESULT.S_OK && frameData.frameAddr != 0)
                            sosFrames.Add(new SOSHelperStackFrame(ip, frameData, stackWalk, SOS, Process.DataTarget));
                        else
                            sosFrames.Add(new SOSManagedStackFrame(ip, sp, stackWalk, SOS));
                    }
                    else
                    {
                        WriteWarning($"Failed to get frame location for thread {threadId}: {hr}");
                    }

                    hr = stackWalk.TryNext();
                } while (hr == HRESULT.S_OK);

                foreach (var frame in sosFrames)
                    WriteObject(frame);
            }
        }

        private HRESULT GetFrameLocation(XCLRDataStackWalk stackWalk, out CLRDATA_ADDRESS ip, out CLRDATA_ADDRESS sp)
        {
            HRESULT hr;

            if (IntPtr.Size == 4)
            {
                hr = stackWalk.TryGetContext<X86_CONTEXT>(ContextFlags.X86ContextAll, out var ctx);

                if (hr == HRESULT.S_OK)
                {
                    ip = ctx.Eip;
                    sp = ctx.Esp;
                }
                else
                {
                    ip = 0;
                    sp = 0;
                }
            }
            else
            {
                hr = stackWalk.TryGetContext<AMD64_CONTEXT>(ContextFlags.AMD64ContextAll, out var ctx);

                if (hr == HRESULT.S_OK)
                {
                    ip = ctx.Rip;
                    sp = ctx.Rsp;
                }
                else
                {
                    ip = 0;
                    sp = 0;
                }
            }

            return hr;
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
