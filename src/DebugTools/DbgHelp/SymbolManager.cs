using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using ChaosLib;
using ClrDebug;
using Microsoft.Diagnostics.Runtime;

namespace DebugTools
{
    class SymbolManager : IDisposable
    {
        private Dictionary<ulong, DbgVtblSymbolInfo> symbolCache = new Dictionary<ulong, DbgVtblSymbolInfo>();

        public Process Process { get; }

        public List<SymbolModule> Modules { get; }

        public SymbolManager(Process process)
        {
            Initialize(process.Handle);

            Process = process;
            Modules = GetModules();
        }

        public SymbolAndModuleInfo GetSymbol(ulong address)
        {
            TryGetSymbol(address, out var result).ThrowOnNotOK();

            return result;
        }

        public HRESULT TryGetSymbol(ulong address, out SymbolAndModuleInfo dbgSymbol)
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

            var hr = DbgHelp.TrySymFromAddr(Process.Handle, (long) address, out var result);

            if (hr != HRESULT.S_OK)
            {
                dbgSymbol = null;
                return hr;
            }

            dbgSymbol = new SymbolAndModuleInfo(result, module);
            return HRESULT.S_OK;
        }

        public DbgVtblSymbolInfo GetVtblSymbol(RcwData rcwData)
        {
            if (symbolCache.TryGetValue(rcwData.VTablePointer, out var existing))
                return existing;

            var offset = 0;

            if (TryGetSymbol(rcwData.VTablePointer, out var vtbl) == HRESULT.CORDBG_E_MODULE_NOT_LOADED)
            {
                var modules = GetModules();

                var existingModules = Modules.Select(m => m.Start).ToArray();

                foreach (var module in modules)
                {
                    if (!existingModules.Contains(module.Start))
                        Modules.Add(module);
                }

                vtbl = GetSymbol(rcwData.VTablePointer);
            }

            //No symbols
            if (vtbl.Symbol.Displacement != 0)
                return null;

            var methods = new List<SymbolAndModuleInfo>();

            //This gets us the methods of CSolution, but not each individual subinterface as we don't have the vtable address of these subinterfaces

            if (vtbl.Symbol.SymbolInfo.Size == 0)
            {
                //The most common way we can hit this scenario is when the symbol is tag PublicSymbol,
                //in which case there's no size, no way to unwrap the symbol, no TypeId, nothing.
                //In this case, we don't seem to have any option but to use brute force

                while (true)
                {
                    var slotAddr = (long) rcwData.VTablePointer + offset;

                    if (offset > 0)
                    {
                        //Getting the symbol of a vtable slot will return the root vtable itself + our offset as its displacement. Therefore,
                        //as long as we don't run into another symbol we know we're still safe
                        var slotSym = GetSymbol((ulong)slotAddr);

                        if (slotSym.Symbol.SymbolInfo.Address != vtbl.Symbol.SymbolInfo.Address)
                            break;
                    }

                    var buffer = Kernel32.ReadProcessMemory(Process.Handle, new IntPtr(slotAddr), IntPtr.Size);

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
                    var buffer = Kernel32.ReadProcessMemory(Process.Handle, new IntPtr((long)rcwData.VTablePointer + offset), IntPtr.Size);

                    var methodAddress = IntPtr.Size == 8 ? BitConverter.ToUInt64(buffer, 0) : BitConverter.ToUInt32(buffer, 0);

                    var method = GetSymbol(methodAddress);

                    methods.Add(method);

                    offset += IntPtr.Size;
                }
            }

            existing = new DbgVtblSymbolInfo(vtbl, methods.ToArray(), rcwData);
            symbolCache[rcwData.VTablePointer] = existing;
            return existing;
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
            var buffer = Ntdll.RtlCreateQueryDebugBuffer();

            var results = new List<SymbolModule>();

            try
            {
                Ntdll.RtlQueryProcessDebugInformation(
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
                Ntdll.RtlDestroyQueryDebugBuffer(buffer);
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
