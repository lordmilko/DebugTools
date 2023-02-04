using System;
using System.Runtime.InteropServices;
using ClrDebug;

namespace DebugTools
{
    public static class NativeExtensions
    {
        #region ReadProcessMemory

        public static byte[] ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            int dwSize)
        {
            byte[] buffer;
            TryReadProcessMemory(hProcess, lpBaseAddress, dwSize, out buffer).ThrowOnNotOK();
            return buffer;
        }

        public static HRESULT TryReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            int dwSize,
            out byte[] buffer)
        {
            var buff = Marshal.AllocHGlobal(dwSize);

            try
            {
                var result = NativeMethods.ReadProcessMemory(
                    hProcess,
                    lpBaseAddress,
                    buff,
                    dwSize,
                    out var lpNumberOfBytesRead
                );

                if (!result)
                {
                    buffer = null;
                    return (HRESULT)Marshal.GetHRForLastWin32Error();
                }

                buffer = new byte[lpNumberOfBytesRead];
                Marshal.Copy(buff, buffer, 0, lpNumberOfBytesRead);
                return HRESULT.S_OK;
            }
            finally
            {
                Marshal.FreeHGlobal(buff);
            }
        }

        #endregion
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

            if (buffer == (RTL_DEBUG_INFORMATION*) 0)
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
