using System;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using DebugTools;

namespace Profiler.Tests
{
    class OutOfProcPowerShellInvoker : PowerShellInvoker
    {
        private Process process;

        public Process Process
        {
            get
            {
                if (process == null)
                {
                    var connectionInfo = powerShell.Runspace.OriginalConnectionInfo;

                    var processInfo = connectionInfo.GetInternalPropertyInfo("Process");

                    process = ((PowerShellProcessInstance)processInfo.GetValue(connectionInfo)).Process;
                }

                return process;
            }
        }

        public OutOfProcPowerShellInvoker() : base(CreateRunspace)
        {
            var modules = initialSessionState.Modules.Select(v => v.Name).ToArray();

            foreach (var module in modules)
                Invoke<object>("Import-Module", new { Name = module });
        }

        private static Runspace CreateRunspace(InitialSessionState initialSessionState)
        {
            var info = CreateRunspaceConnectionInfo(null);

            return RunspaceFactory.CreateRunspace(info);
        }

        private static RunspaceConnectionInfo CreateRunspaceConnectionInfo(PSCredential credential)
        {
            //Stores a Process object. The Process will be started when the Runsapce is started, and automatically when the parent
            //process (i.e. this process) ends. Support for auto-detecting PowerShell version was only added in PowerShell 7,
            //so we have to tell it the path to the PowerShell executable we want to launch
            var processInstance = new PowerShellProcessInstance(new Version(5, 1, 0, 0), credential, null, false);
            processInstance.Process.StartInfo.FileName = "C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe";
            processInstance.Process.EnableRaisingEvents = true;

            var psi = processInstance.Process.StartInfo;

            //When running as SYSTEM, attempting to launch PowerShell with -NoLogo gives the error "Version v2.0.50727 of the .NET Framework is not installed and it is required to run version 2.0 of Windows PowerShell."
            //when -s is not last (-s seems to be a special parameter to run PowerShell as a server that reads/writes to stdin/stdout). Solution: make -s last!
            psi.Arguments = psi.Arguments.Replace("-s ", string.Empty) + " -s";

            var type = typeof(RunspaceConnectionInfo).Assembly.GetType("System.Management.Automation.Runspaces.NewProcessConnectionInfo");

            if (type == null)
                throw new InvalidOperationException("Cannot find type NewProcessConnectionInfo");

            var internalFlags = BindingFlags.Instance | BindingFlags.NonPublic;

            var ctor = type.GetConstructor(internalFlags, null, new[] { typeof(PSCredential) }, null);

            if (ctor == null)
                throw new InvalidOperationException("Cannot find the right constructor on NewProcessConnectionInfo");

            var obj = (RunspaceConnectionInfo)ctor.Invoke(new object[] { null });

            var processInfo = type.GetProperty("Process", internalFlags);

            if (processInfo == null)
                throw new InvalidOperationException("Cannot find the Process property on NewProcessConnectionInfo");

            processInfo.SetValue(obj, processInstance);

            return obj;
        }
    }
}
