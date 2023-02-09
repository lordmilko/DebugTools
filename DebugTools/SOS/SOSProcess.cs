using System;
using System.Diagnostics;
using ClrDebug;
using static ClrDebug.Extensions;

namespace DebugTools.SOS
{
    //SOSProcess gives these crazy errors about channel sinks when i return it. We didn't create it via the RemoteExecutor, so we just work around this by creating a serializable handle instead
    [Serializable]
    public struct SOSProcessHandle
    {
        public int ProcessId { get; }

        public SOSProcessHandle(int processId)
        {
            ProcessId = processId;
        }
    }

    internal class SOSProcess
    {
        public int ProcessId => Process.Id;

        internal Process Process { get; }

        internal SOSDacInterface SOS { get; }

        internal DataTarget DataTarget { get; }

        public SOSProcess(Process process)
        {
            Process = process;

            DataTarget = new DataTarget(process);
            SOS = CLRDataCreateInstance(DataTarget).SOSDacInterface;

            var xclrProcess = SOS.As<XCLRDataProcess>();

            DataTarget.SetFlushCallback(() => xclrProcess.Flush());
        }
    }
}
