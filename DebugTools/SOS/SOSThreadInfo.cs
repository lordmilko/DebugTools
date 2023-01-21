using ClrDebug;

namespace DebugTools.SOS
{
    public class SOSThreadInfo
    {
        public int ThreadId { get; }

        public ThreadState State { get; }

        public string Mode { get; }

        public CLRDATA_ADDRESS AppDomain { get; }

        public int LockCount { get; }

        public SOSThreadInfo(DacpThreadData thread, SOSDacInterface sos)
        {
            ThreadId = thread.osThreadId;
            State = thread.state;
            Mode = thread.preemptiveGCDisabled ? "Cooperative" : "Preemptive";

            if (thread.domain != 0)
                AppDomain = thread.domain;
            else
            {
                if (sos.TryGetDomainFromContext(thread.context, out var domain) == HRESULT.S_OK)
                    AppDomain = domain;
            }

            LockCount = thread.lockCount;
        }
    }
}
