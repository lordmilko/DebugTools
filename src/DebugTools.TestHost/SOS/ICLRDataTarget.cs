using System;
using System.Runtime.InteropServices;

namespace DebugTools.TestHost
{
    [Guid("3E11CCEE-D08B-43E5-AF01-32717A64DA03")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface ICLRDataTarget
    {
        [PreserveSig]
        uint GetMachineType(
            [Out] out IMAGE_FILE_MACHINE machineType);

        [PreserveSig]
        uint GetPointerSize(
            [Out] out int pointerSize);

        [PreserveSig]
        uint GetImageBase(
            [MarshalAs(UnmanagedType.LPWStr), In] string imagePath,
            [Out] out ulong baseAddress);

        [PreserveSig]
        uint ReadVirtual(
            [In] ulong address,
            [Out] IntPtr buffer,
            [In] uint bytesRequested,
            [Out] out uint bytesRead);

        [PreserveSig]
        uint WriteVirtual(
            [In] ulong address,
            [In] IntPtr buffer,
            [In] int bytesRequested,
            [Out] out int bytesWritten);

        [PreserveSig]
        uint GetTLSValue(
            [In] int threadID,
            [In] int index,
            [Out] out ulong value);

        [PreserveSig]
        uint SetTLSValue(
            [In] int threadID,
            [In] int index,
            [In] ulong value);

        [PreserveSig]
        uint GetCurrentThreadID(
            [Out] out int threadID);

        [PreserveSig]
        uint GetThreadContext(
            [In] int threadID,
            [In] uint contextFlags,
            [In] int contextSize,
            [Out] IntPtr context);

        [PreserveSig]
        uint SetThreadContext(
            [In] int threadID,
            [In] int contextSize,
            [In] IntPtr context);

        [PreserveSig]
        uint Request(
            [In] uint reqCode,
            [In] int inBufferSize,
            [In] IntPtr inBuffer,
            [In] int outBufferSize,
            [Out] IntPtr outBuffer);
    }
}
