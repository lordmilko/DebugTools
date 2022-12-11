using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace DebugTools.PowerShell
{
    internal static class NativeMethods
    {
        private const string Kernel32 = "kernel32.dll";
        private const string Ole32 = "ole32.dll";

        internal const int S_FALSE = 1;

        [DllImport(Kernel32, SetLastError = true)]
        public static extern bool CloseHandle(IntPtr handle);

        [DllImport(Kernel32, SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern bool CreateProcessA(
            string lpApplicationName,
            string lpCommandLine,
            ref SECURITY_ATTRIBUTES lpProcessAttributes,
            ref SECURITY_ATTRIBUTES lpThreadAttributes,
            bool bInheritHandles,
            CreateProcessFlags dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            [In] ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        #region ole32.dll

        [DllImport(Ole32, SetLastError = true)]
        public static extern int CoRegisterMessageFilter(IMessageFilter messageFilter, out IMessageFilter oldMessageFilter);

        [DllImport(Ole32, PreserveSig = false)]
        public static extern void CreateBindCtx(int reserved, [MarshalAs(UnmanagedType.Interface)] out IBindCtx bindContext);

        [DllImport(Ole32, PreserveSig = false)]
        public static extern void GetRunningObjectTable(int reserved, [MarshalAs(UnmanagedType.Interface)] out IRunningObjectTable runningObjectTable);

        #endregion
    }
}