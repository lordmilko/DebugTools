using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ChaosLib;
using ChaosLib.Memory;
using ChaosLib.Metadata;
using ClrDebug;
using DebugTools.Profiler;
using MethodInfo = System.Reflection.MethodInfo;
using ChaosPEFile = ChaosLib.Metadata.PEFile;

namespace DebugTools.Memory
{
    public class DllInjector
    {
        private Process process;
        private DataTarget dataTarget;
        private DataTargetMemoryStream dataTargetStream;
        private Dictionary<string, ChaosPEFile> peFiles = new Dictionary<string, ChaosPEFile>();

        public DllInjector(Process process)
        {
            this.process = process;
            dataTarget = new DataTarget(process);
            dataTargetStream = new DataTargetMemoryStream(dataTarget);
        }

        public void Inject(MethodInfo method, string parameter = null)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));

            using (var hProcess = Kernel32.OpenProcess(ProcessAccessFlags.All, false, process.Id))
            {
                bool is32Bit = Kernel32.IsWow64Process(hProcess);

                var nativeDll = is32Bit ? ProfilerInfo.Nativex86 : ProfilerInfo.Nativex64;

                EnsureDllLoaded(hProcess, nativeDll);
                InvokeDllExport(hProcess, method, nativeDll, is32Bit, parameter);
            }
        }

        private void EnsureDllLoaded(IntPtr hProcess, string nativeDll)
        {
            if (!TryGetDllBase(nativeDll, out _))
            {
                var loadLibrary = GetRemoteExport("kernel32.dll", "LoadLibraryA");

                var bytes = Encoding.ASCII.GetBytes(nativeDll + char.MinValue);

                //Load the library
                using (var memory = new VirtualAlloc(hProcess, bytes.Length, AllocationType.Commit, MemoryProtection.ReadWrite))
                {
                    //Write the argument for LoadLibrary
                    dataTarget.WriteVirtual(memory, bytes);

                    //Call LoadLibrary
                    Kernel32.CreateRemoteThread(hProcess, loadLibrary.FunctionAddress, memory, true);
                }
            }
        }

        private void InvokeDllExport(IntPtr hProcess, MethodInfo method, string nativeDll, bool is32Bit, string parameter)
        {
            var callManaged = GetRemoteExport(Path.GetFileName(nativeDll), "CallManaged");

            ValidateMethodInfo(method);

            var argBuilder = new UnmanagedArgBuilder(is32Bit)
            {
                method.DeclaringType.Assembly.Location,
                method.DeclaringType.FullName,
                method.Name,
                parameter ?? string.Empty
            };

            //Call the export in the library we've now loaded
            using (var memory = new VirtualAlloc(hProcess, argBuilder.Length, AllocationType.Commit, MemoryProtection.ReadWrite))
            {
                var bytes = argBuilder.GetBytes(memory);

                //Write the arguments for the export
                dataTarget.WriteVirtual(memory, bytes);

                //Call the export
                //We always have to wait for finish, because we're going to blow away the allocated memory region after this returns
                var hr = (HRESULT) Kernel32.CreateRemoteThread(hProcess, callManaged.FunctionAddress, memory, true);

                hr.ThrowOnNotOK();
            }
        }

        private void ValidateMethodInfo(MethodInfo method)
        {
            if (!method.IsStatic)
                throw new InvalidOperationException("Expected a static method");

            if (!method.IsPublic)
                throw new InvalidOperationException("Expected a public method");

            var parameters = method.GetParameters();

            if (parameters.Length != 1 || parameters[0].ParameterType != typeof(string))
                throw new InvalidOperationException("Expected a method with exactly one parameter of type 'string'.");

            if (method.ReturnType != typeof(int))
                throw new InvalidOperationException("Expected a method that returns 'int'.");
        }

        private void UnloadDll(IntPtr hProcess, string dll)
        {
            if (TryGetDllBase(dll, out var baseAddress))
            {
                var freeLibrary = GetRemoteExport("kernel32.dll", "FreeLibrary");

                Kernel32.CreateRemoteThread(hProcess, freeLibrary.FunctionAddress, baseAddress, true);
            }
        }

        private bool TryGetDllBase(string dll, out IntPtr baseAddress)
        {
            var moduleName = Path.GetFileName(dll);

            var modules = Process.GetProcessById(process.Id).Modules;

            var result = modules.Cast<ProcessModule>().FirstOrDefault(m => moduleName.Equals(m.ModuleName, StringComparison.OrdinalIgnoreCase));

            if (result != null)
            {
                baseAddress = result.BaseAddress;
                return true;
            }

            baseAddress = default;
            return false;
        }

        private ImageExportInfo GetRemoteExport(string moduleName, string exportName)
        {
            var peFile = GetPEFile(moduleName);

            var export = peFile.ExportDirectory.Exports.FirstOrDefault(e => exportName.Equals(e.Name, StringComparison.OrdinalIgnoreCase));

            if (export == null)
                throw new InvalidOperationException($"Could not find export {moduleName}!{exportName}");

            if (export is ImageForwardedExportInfo f)
                throw new NotImplementedException("Resolving forwarded exports is not implemented");

            return (ImageExportInfo) export;
        }

        private ChaosPEFile GetPEFile(string name)
        {
            if (peFiles.TryGetValue(name, out var existing))
                return existing;

            var modules = Process.GetProcessById(process.Id).Modules;

            var match = modules.Cast<ProcessModule>().FirstOrDefault(m => name.Equals(m.ModuleName, StringComparison.OrdinalIgnoreCase));

            if (match == null)
                throw new InvalidOperationException($"Could not find module '{name}'.");

            existing = GetPEFile(match);
            peFiles[name] = existing;
            return existing;
        }

        private ChaosPEFile GetPEFile(ProcessModule module)
        {
            var relativeStream = new RelativeToAbsoluteStream(dataTargetStream, (long)module.BaseAddress);
            relativeStream.Seek(0, SeekOrigin.Begin);

            var peFile = new ChaosPEFile(relativeStream, true);

            return peFile;
        }
    }
}
