using System;
using System.Runtime.InteropServices;

namespace DebugTools.TestHost
{
    delegate uint CLRDataCreateInstanceDelegate(
        [In, MarshalAs(UnmanagedType.LPStruct)] Guid iid,
        [In, MarshalAs(UnmanagedType.Interface)] ICLRDataTarget target,
        [MarshalAs(UnmanagedType.IUnknown), Out] out object iface);

    delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    static class NativeMethods
    {
        private const string kernel32 = "kernel32.dll";
        private const string user32 = "user32.dll";

        [DllImport(user32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport(kernel32, SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport(kernel32, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "LoadLibraryW")]
        public static extern IntPtr LoadLibrary(string lpLibFileName);

        [DllImport(kernel32, SetLastError = true)]
        public static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            [Out] IntPtr lpBuffer,
            int dwSize,
            out uint lpNumberOfBytesRead);
    }
}
