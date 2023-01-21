using System.Diagnostics;
using ClrDebug;
using static ClrDebug.Extensions;

namespace DebugTools.SOS
{
    public class SOSProcess
    {
        public Process Process { get; }

        public SOSDacInterface SOS { get; }

        public DataTarget DataTarget { get; }

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
