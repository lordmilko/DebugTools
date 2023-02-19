using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DebugTools
{
    [ComConversionLoss]
    [InterfaceType(1)]
    [Guid("00000016-0000-0000-C000-000000000046")]
    [ComImport]
    public interface IMessageFilter
    {
        [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        uint HandleInComingCall(
            [In] uint dwCallType,
            [In] IntPtr htaskCaller,
            [In] uint dwTickCount,
            [MarshalAs(UnmanagedType.LPArray), In] INTERFACEINFO[] lpInterfaceInfo);

        [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        uint RetryRejectedCall([In] IntPtr htaskCallee, [In] uint dwTickCount, [In] uint dwRejectType);

        [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        uint MessagePending([In] IntPtr htaskCallee, [In] uint dwTickCount, [In] uint dwPendingType);
    }
}