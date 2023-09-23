using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using ChaosLib;
using ClrDebug;
using DebugTools.Memory;
using static ClrDebug.Extensions;

namespace DebugTools.SOS
{
    internal class SOSProcess
    {
        public int ProcessId => Process.Id;

        internal Process Process { get; }

        internal SOSDacInterface SOS { get; }

        internal DataTarget DataTarget { get; }

        public SOSProcess(Process process)
        {
            Process = process;

            DataTarget = new DataTarget(process);
            SOS = GetSOSDacInterface(DataTarget);

            var xclrProcess = SOS.As<XCLRDataProcess>();

            DataTarget.SetFlushCallback(() => xclrProcess.Flush());
        }

        private SOSDacInterface GetSOSDacInterface(DataTarget dataTarget)
        {
            //Get a new Process so we get the latest list of modules. Modules won't refresh on a Process object after they've been
            //retrieved the first time
            var process = Process.GetProcessById(ProcessId);

            var modules = process.Modules.Cast<ProcessModule>().ToArray();

            var clr = modules.FirstOrDefault(m => m.ModuleName.Equals("clr.dll", StringComparison.OrdinalIgnoreCase));

            if (clr != null)
                return CLRDataCreateInstance(dataTarget).SOSDacInterface;

            var coreclr = modules.FirstOrDefault(m => m.ModuleName.Equals("coreclr.dll", StringComparison.OrdinalIgnoreCase));

            if (coreclr == null)
                throw new InvalidOperationException($"Could not find module clr.dll or coreclr.dll on process {Process.Id}.");

            return CoreCLRDataCreateInstance(coreclr, dataTarget).SOSDacInterface;
        }

        private CLRDataCreateInstanceInterfaces CoreCLRDataCreateInstance(ProcessModule module, DataTarget dataTarget)
        {
            var dacPath = Path.Combine(Path.GetDirectoryName(module.FileName), "mscordaccore.dll");

            if (!File.Exists(dacPath))
                throw new FileNotFoundException($"Cannot find file '{dacPath}'.");

            var dacLib = Kernel32.LoadLibrary(dacPath);

            var clrDataCreateInstancePtr = Kernel32.GetProcAddress(dacLib, "CLRDataCreateInstance");

            var clrDataCreateInstanceDelegate = Marshal.GetDelegateForFunctionPointer<CLRDataCreateInstanceDelegate>(clrDataCreateInstancePtr);

            var clrDataCreateInstance = CLRDataCreateInstance(clrDataCreateInstanceDelegate, dataTarget);

            return clrDataCreateInstance;
        }
    }
}
