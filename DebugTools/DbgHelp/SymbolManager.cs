using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ClrDebug;
using ClrDebug.DbgEng;

namespace DebugTools
{
    class SymbolManager : IDisposable
    {
        public Process Process { get; }

        public List<SymbolModule> Modules { get; }

        public SymbolManager(Process process)
        {
            Initialize(process.Handle);

            Process = process;
            Modules = GetModules();
        }

        public DbgSymbolInfo GetSymbol(ulong address)
        {
            TryGetSymbol(address, out var result).ThrowOnNotOK();

            return result;
        }

        public HRESULT TryGetSymbol(ulong address, out DbgSymbolInfo dbgSymbol)
        {
            /* DbgHelp tends not to comply as nicely as you'd like if you don't load every module immedaitely as part of SymInitialize. Even if loading
             * your modules later were reliable, this is still extremely slow when you have a lot of modules (like Visual Studio) or compiled *.ni.dll .NET executables
             * (like Visual Studio). As such, to work around these issues we implement our own module loader, copying the exact same techniques that are used in DbgHelp.
             * Then, rather than loading all of the modules at once, we load each module on demand as required when we can see the target address resides inside of it. */

            if (!TryGetModuleFromAddress(address, out var module))
            {
                dbgSymbol = null;
                return HRESULT.CORDBG_E_MODULE_NOT_LOADED;
            }

            if (!module.SymbolsLoaded)
            {
                LoadModuleSymbols(module);
            }

            var hr = DbgHelp.TrySymFromAddr(Process.Handle, address, out var result);

            if (hr != HRESULT.S_OK)
            {
                dbgSymbol = null;
                return hr;
            }

            dbgSymbol = new DbgSymbolInfo(result, module);
            return HRESULT.S_OK;
        }

        private SymTag GetSymTag(ulong moduleBase, int typeId)
        {
            var result = NativeMethods.SymGetTypeInfo(
                Process.Handle,
                moduleBase,
                typeId,
                IMAGEHLP_SYMBOL_TYPE_INFO.TI_GET_SYMTAG,
                out var info
            );

            if (!result)
                throw new InvalidOperationException($"Failed to get tag from module {moduleBase} type {typeId}");

            var tag = (SymTag)info.ToInt32();

            return tag;
        }

        private int? GetTypeId(ulong moduleBase, int typeId)
        {
            var result = NativeMethods.SymGetTypeInfo(
                Process.Handle,
                moduleBase,
                typeId,
                IMAGEHLP_SYMBOL_TYPE_INFO.TI_GET_TYPEID,
                out var info
            );

            if (!result)
                return null;

            return info.ToInt32();
        }

        public DbgVtblSymbolInfo GetVtblSymbol(ulong address)
        {
            var offset = 0;

            var vtbl = GetSymbol(address);

            //No symbols
            if (vtbl.Symbol.Displacement != 0)
                return null;

            var methods = new List<DbgSymbolInfo>();

            if (vtbl.Symbol.SymbolInfo.Size == 0)
            {
                //The most common way we can hit this scenario is when the symbol is tag PublicSymbol,
                //in which case there's no size, no way to unwrap the symbol, no TypeId, nothing.
                //In this case, we don't seem to have any option but to use brute force

                while (true)
                {
                    var slotAddr = (long) address + offset;

                    if (offset > 0)
                    {
                        //Getting the symbol of a vtable slot will return the root vtable itself + our offset as its displacement. Therefore,
                        //as long as we don't run into another symbol we know we're still safe
                        var slotSym = GetSymbol((ulong)slotAddr);

                        if (slotSym.Symbol.SymbolInfo.Address != vtbl.Symbol.SymbolInfo.Address)
                            break;
                    }

                    var buffer = NativeExtensions.ReadProcessMemory(Process.Handle, new IntPtr(slotAddr), IntPtr.Size);

                    var methodAddress = IntPtr.Size == 8 ? BitConverter.ToUInt64(buffer, 0) : BitConverter.ToUInt32(buffer, 0);

                    var method = GetSymbol(methodAddress);

                    methods.Add(method);

                    offset += IntPtr.Size;
                }
            }
            else
            {
                while (offset < vtbl.Symbol.SymbolInfo.Size)
                {
                    var buffer = NativeExtensions.ReadProcessMemory(Process.Handle, new IntPtr((long)address + offset), IntPtr.Size);

                    var methodAddress = IntPtr.Size == 8 ? BitConverter.ToUInt64(buffer, 0) : BitConverter.ToUInt32(buffer, 0);

                    var method = GetSymbol(methodAddress);

                    methods.Add(method);

                    offset += IntPtr.Size;
                }
            }

            return new DbgVtblSymbolInfo(vtbl, methods.ToArray());
        }

        private bool TryGetModuleFromAddress(ulong address, out SymbolModule result)
        {
            foreach (var module in Modules)
            {
                if (address == module.Start && module.Length == 0)
                {
                    result = module;
                    return true;
                }

                if (address >= module.Start && address < module.End)
                {
                    result = module;
                    return true;
                }
            }

            result = null;
            return false;
        }

        private void LoadModuleSymbols(SymbolModule module)
        {
            DbgHelp.SymLoadModuleEx(
                Process.Handle,
                imageName: module.Name,
                baseOfDll: module.Start,
                dllSize: module.Length
            );

            module.SymbolsLoaded = true;
        }

        private unsafe List<SymbolModule> GetModules()
        {
            var buffer = NativeExtensions.RtlCreateQueryDebugBuffer();

            var results = new List<SymbolModule>();

            try
            {
                NativeExtensions.RtlQueryProcessDebugInformation(
                    Process.Id,
                    RtlQueryProcessFlag.Modules | RtlQueryProcessFlag.NonInvasive,
                    buffer
                );

                //Skip over NumberOfModules (factoring in padding. NumberOfModules is 4 bytes, +0 bytes padding on x86, +4 on x64)
                var baseAddr = (long)buffer->Modules + IntPtr.Size;
                var infoSize = Marshal.SizeOf<RTL_PROCESS_MODULE_INFORMATION>();

                for (var i = 0; i < buffer->Modules->NumberOfModules; i++)
                {
                    var item = (RTL_PROCESS_MODULE_INFORMATION*)(baseAddr + (infoSize * i));

                    results.Add(new SymbolModule(item));
                }

                return results;
            }
            finally
            {
                NativeExtensions.RtlDestroyQueryDebugBuffer(buffer);
            }
        }

        private static void Initialize(IntPtr process)
        {
            DbgHelp.SymInitialize(process);
        }

        public void Dispose()
        {
            if (Process != null)
                DbgHelp.SymCleanup(Process.Handle);
        }
    }
}
