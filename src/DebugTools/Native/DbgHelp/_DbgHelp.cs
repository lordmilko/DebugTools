﻿using System;
using System.Runtime.InteropServices;
using ClrDebug;

namespace DebugTools
{
    //SetDllDirectory is called in HostApp (before ClrMD is loaded) to set DLL directory to Debugging Tools for Windows folder
    class DbgHelp
    {
        private const int MaxNameLength = 2000; //2000 characters

        static class NativeMethods
        {
            private const string dbghelp = "dbghelp.dll";

            [DllImport(dbghelp)]
            internal static extern bool SymCleanup(
                [In] IntPtr hProcess);

            [DllImport(dbghelp, EntryPoint = "SymInitializeW", SetLastError = true)]
            internal static extern bool SymInitialize(
                [In] IntPtr hProcess,
                [In] string UserSearchPath,
                [In] bool fInvadeProcess);

            [DllImport(dbghelp, SetLastError = true)]
            internal static extern bool SymFromAddr(
                [In] IntPtr hProcess,
                [In] ulong address,
                [Out] out long displacement,
                [Out] IntPtr pSymbolInfo);

            [DllImport(dbghelp, SetLastError = true)]
            public static extern bool SymGetTypeInfo(
                [In] IntPtr hProcess,
                [In] ulong ModBase,
                [In] int TypeId,
                [In] IMAGEHLP_SYMBOL_TYPE_INFO GetType,
                [Out] out IntPtr pInfo);

            [DllImport(dbghelp, SetLastError = true)]
            internal static extern ulong SymLoadModuleExW(
                [In] IntPtr hProcess,
                [In, Optional] IntPtr hFile,
                [In, Optional, MarshalAs(UnmanagedType.LPWStr)] string ImageName,
                [In, Optional, MarshalAs(UnmanagedType.LPWStr)] string ModuleName,
                [In, Optional] ulong BaseOfDll,
                [In, Optional] int DllSize,
                [In, Optional] IntPtr Data,
                [In, Optional] int Flags
            );
        }

        #region SymInitialize

        public static void SymInitialize(IntPtr hProcess, string userSearchPath = null, bool invadeProcess = false) =>
            TrySymInitialize(hProcess, userSearchPath, invadeProcess).ThrowOnNotOK();

        public static HRESULT TrySymInitialize(IntPtr hProcess, string userSearchPath = null, bool invadeProcess = false)
        {
            var result = NativeMethods.SymInitialize(hProcess, userSearchPath, invadeProcess);

            if (!result)
                return (HRESULT) Marshal.GetHRForLastWin32Error();

            return HRESULT.S_OK;
        }

        #endregion
        #region SymCleanup

        public static void SymCleanup(IntPtr hProcess) => TrySymCleanup(hProcess).ThrowOnNotOK();

        public static HRESULT TrySymCleanup(IntPtr hProcess)
        {
            var result = NativeMethods.SymCleanup(hProcess);

            if (!result)
                return (HRESULT)Marshal.GetHRForLastWin32Error();

            return HRESULT.S_OK;
        }

        #endregion
        #region SymFromAddr

        public static SymFromAddrResult SymFromAddr(IntPtr hProcess, ulong address)
        {
            SymFromAddrResult result;
            TrySymFromAddr(hProcess, address, out result).ThrowOnNotOK();
            return result;
        }

        public static unsafe HRESULT TrySymFromAddr(IntPtr hProcess, ulong address, out SymFromAddrResult result)
        {
            IntPtr buffer = IntPtr.Zero;

            try
            {
                buffer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(SYMBOL_INFO)) + MaxNameLength);

                SYMBOL_INFO* pNative = (SYMBOL_INFO*) buffer;
                pNative->SizeOfStruct = Marshal.SizeOf(typeof(SYMBOL_INFO));
                pNative->MaxNameLen = MaxNameLength; // Characters, not bytes!

                long displacement;
                var innerResult = NativeMethods.SymFromAddr(hProcess, address, out displacement, buffer);

                if (!innerResult)
                {
                    result = default;
                    return (HRESULT)Marshal.GetHRForLastWin32Error();
                }

                result = new SymFromAddrResult(displacement, new SymbolInfo(pNative));
                return HRESULT.S_OK;
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                    Marshal.FreeHGlobal(buffer);
            }
        }

        #endregion
        #region SymLoadModuleEx

        public static void SymLoadModuleEx(
            IntPtr hProcess,
            IntPtr hFile = default,
            string imageName = null,
            string moduleName = null,
            ulong baseOfDll = 0,
            int dllSize = 0,
            IntPtr data = default,
            int flags = 0
        ) => TrySymLoadModuleEx(hProcess, hFile, imageName, moduleName, baseOfDll, dllSize, data, flags).ThrowOnNotOK();

        public static HRESULT TrySymLoadModuleEx(
            IntPtr hProcess,
            IntPtr hFile = default,
            string imageName = null,
            string moduleName = null,
            ulong baseOfDll = 0,
            int dllSize = 0,
            IntPtr data = default,
            int flags = 0
            )
        {
            var result = NativeMethods.SymLoadModuleExW(
                hProcess,
                hFile,
                imageName,
                moduleName,
                baseOfDll,
                dllSize,
                data,
                flags
            );

            if (result == 0)
            {
                var hr = Marshal.GetHRForLastWin32Error();

                //Module was already loaded
                if (hr == 0)
                    return HRESULT.S_FALSE;

                return (HRESULT)hr;
            }

            return HRESULT.S_OK;
        }

        #endregion
    }
}