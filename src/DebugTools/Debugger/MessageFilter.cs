using System;

namespace DebugTools
{
    /// <summary>
    /// Allows performing out-of-proc COM over RPC calls without experiencing exceptions.<para/>
    /// For more information see https://docs.microsoft.com/en-us/previous-versions/visualstudio/visual-studio-2010/ms228772(v=vs.100)?redirectedfrom=MSDN
    /// </summary>
    public class MessageFilter : IMessageFilter, IDisposable
    {
        private const uint CancelCall = ~0U;

        private readonly TimeSpan _timeout;
        private readonly TimeSpan _retryDelay;

        private readonly IMessageFilter oldFilter;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageFilter"/> class.
        /// </summary>
        public MessageFilter()
            : this(timeout: TimeSpan.FromSeconds(60), retryDelay: TimeSpan.FromMilliseconds(150))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageFilter"/> with a timeout and retry delay duration.
        /// </summary>
        /// <param name="timeout">The amount of time to wait when retrying before cancelling the COM call.</param>
        /// <param name="retryDelay">The amount of time to wait before retrying a failed COM call.</param>
        public MessageFilter(TimeSpan timeout, TimeSpan retryDelay)
        {
            _timeout = timeout;
            _retryDelay = retryDelay;

            NativeMethods.CoRegisterMessageFilter(this, out oldFilter);
        }

        void IDisposable.Dispose()
        {
            IMessageFilter oldOldFilter = null;

            NativeMethods.CoRegisterMessageFilter(oldFilter, out oldOldFilter);
        }

        uint IMessageFilter.HandleInComingCall(uint dwCallType, IntPtr htaskCaller, uint dwTickCount, INTERFACEINFO[] lpInterfaceInfo)
        {
            return (uint)SERVERCALL.SERVERCALL_ISHANDLED;
        }

        uint IMessageFilter.RetryRejectedCall(IntPtr htaskCallee, uint dwTickCount, uint dwRejectType)
        {
            if ((SERVERCALL)dwRejectType != SERVERCALL.SERVERCALL_RETRYLATER
                && (SERVERCALL)dwRejectType != SERVERCALL.SERVERCALL_REJECTED)
            {
                return CancelCall;
            }

            if (dwTickCount >= _timeout.TotalMilliseconds)
            {
                return CancelCall;
            }

            return (uint)_retryDelay.TotalMilliseconds;
        }

        uint IMessageFilter.MessagePending(IntPtr htaskCallee, uint dwTickCount, uint dwPendingType)
        {
            return (uint)PENDINGMSG.PENDINGMSG_WAITDEFPROCESS;
        }
    }
}
