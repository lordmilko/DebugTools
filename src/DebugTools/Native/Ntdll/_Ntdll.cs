using System.Runtime.InteropServices;
using ClrDebug;

namespace DebugTools
{
    static class Ntdll
    {
        static class NativeMethods
        {
            private const string ntdll = "ntdll.dll";

            [DllImport(ntdll)]
            public static extern unsafe RTL_DEBUG_INFORMATION* RtlCreateQueryDebugBuffer(
                [In] int MaximumCommit,
                [In] bool UseEventPair);

            [DllImport(ntdll)]
            public static extern unsafe int RtlDestroyQueryDebugBuffer(
                [In] RTL_DEBUG_INFORMATION* Buffer);

            [DllImport(ntdll)]
            public static extern unsafe int RtlQueryProcessDebugInformation(
                [In] int UniqueProcessId,
                [In] RtlQueryProcessFlag DebugInfoClassMask,
                [In, Out] RTL_DEBUG_INFORMATION* DebugBuffer);
        }

        #region RtlCreateQueryDebugBuffer

        public static unsafe RTL_DEBUG_INFORMATION* RtlCreateQueryDebugBuffer(
            int maximumCommit = 0,
            bool useEventPair = false)
        {
            TryRtlCreateQueryDebugBuffer(out var buffer, maximumCommit, useEventPair).ThrowOnNotOK();

            return buffer;
        }

        public static unsafe HRESULT TryRtlCreateQueryDebugBuffer(
            out RTL_DEBUG_INFORMATION* buffer,
            int maximumCommit = 0,
            bool useEventPair = false)
        {
            buffer = NativeMethods.RtlCreateQueryDebugBuffer(maximumCommit, useEventPair);

            if (buffer == (RTL_DEBUG_INFORMATION*)0)
                return HRESULT.ERROR_NOT_ENOUGH_MEMORY;

            return HRESULT.S_OK;
        }

        #endregion
        #region RtlDestroyQueryDebugBuffer

        public static unsafe void RtlDestroyQueryDebugBuffer(RTL_DEBUG_INFORMATION* buffer) =>
            TryRtlDestroyQueryDebugBuffer(buffer).ThrowOnNotOK();

        public static unsafe HRESULT TryRtlDestroyQueryDebugBuffer(RTL_DEBUG_INFORMATION* buffer)
        {
            var result = NativeMethods.RtlDestroyQueryDebugBuffer(buffer);

            if (result == 0)
                return HRESULT.S_OK;

            return HRESULT.E_FAIL;
        }

        #endregion
        #region RtlQueryProcessDebugInformation

        public static unsafe void RtlQueryProcessDebugInformation(
            int processId,
            RtlQueryProcessFlag debugInfoClassMask,
            RTL_DEBUG_INFORMATION* debugBuffer) =>
            TryRtlQueryProcessDebugInformation(processId, debugInfoClassMask, debugBuffer).ThrowOnNotOK();

        public static unsafe HRESULT TryRtlQueryProcessDebugInformation(
            int processId,
            RtlQueryProcessFlag debugInfoClassMask,
            RTL_DEBUG_INFORMATION* debugBuffer)
        {
            var result = NativeMethods.RtlQueryProcessDebugInformation(processId, debugInfoClassMask, debugBuffer);

            if (result == 0)
                return HRESULT.S_OK;

            return HRESULT.E_FAIL;
        }

        #endregion
    }
}
