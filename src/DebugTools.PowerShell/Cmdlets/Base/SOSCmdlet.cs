using System;
using System.Globalization;
using System.Management.Automation;
using System.Reflection;
using ClrDebug;
using DebugTools.Host;
using DebugTools.Profiler;
using DebugTools.SOS;

namespace DebugTools.PowerShell.Cmdlets
{
    public abstract class SOSCmdlet : DebugToolsCmdlet
    {
        [Parameter(Mandatory = false, ValueFromPipeline = true)]
        public LocalSOSProcess Process { get; set; }

        protected Either<string, CLRDATA_ADDRESS>[] nameOrAddress;

        protected HostApp HostApp { get; private set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Dbg { get; set; }

        internal new string ParameterSetName
        {
            get => base.ParameterSetName;
            set
            {
                var method = GetType().GetMethod("SetParameterSetName", BindingFlags.Instance | BindingFlags.NonPublic);

                method.Invoke(this, new[] {value});
            }
        }

        protected virtual void ProcessDefault()
        {
            var appDomains = HostApp.GetSOSAppDomains(Process);

            foreach (var appDomain in appDomains)
                ProcessAppDomain(appDomain);
        }

        protected virtual void ProcessAppDomain(SOSAppDomain appDomain)
        {
            var assemblies = HostApp.GetSOSAssemblies(Process, appDomain);

            foreach (var assembly in assemblies)
                ProcessAssembly(assembly);
        }

        protected virtual void ProcessAssembly(SOSAssembly assembly)
        {
            var modules = HostApp.GetSOSModules(Process, assembly);

            foreach (var module in modules)
                ProcessModule(module);
        }

        protected virtual void ProcessModule(SOSModule module)
        {
            var methodTables = HostApp.GetSOSMethodTables(Process, module);

            foreach (var methodTable in methodTables)
                ProcessMethodTable(methodTable);
        }

        protected virtual void ProcessMethodTable(SOSMethodTable methodTable)
        {
        }

        protected sealed override void ProcessRecord()
        {
            //It seems like the parameter set gets reset whenever ProcessRecord is called, so we have to call this here
            FixupNameOrAddress();

            if (Process == null)
            {
                if (DebugToolsSessionState.SOSProcesses.Count == 0 && DebugToolsSessionState.ProfilerSessions.Count > 0)
                {
                    var session = DebugToolsSessionState.GetImplicitProfilerSession();

                    if (session.Type == ProfilerSessionType.XmlFile)
                        throw new InvalidOperationException($"Cannot execute cmdlet: no -Session was specified and no global Session could be found in the PowerShell session.");

                    var process = ((LiveProfilerTarget) session.Target).Process;

                    HostApp = DebugToolsSessionState.GetDetectedHost(process, Dbg);

                    Process = new LocalSOSProcess(HostApp.CreateSOSProcess(process.Id, false));
                }
                else
                {
                    Process = DebugToolsSessionState.GetImplicitSOSProcess();

                    HostApp = DebugToolsSessionState.GetDetectedHost(Process.Process, Dbg);
                }
            }
            else
            {
                HostApp = DebugToolsSessionState.GetDetectedHost(Process.Process, Dbg);
            }

            ProcessRecordEx();
        }

        private void FixupNameOrAddress()
        {
            //Get-SOSAssembly 1 -> AddressSet
            //Get-SOSAssembly foo -> AddressSet
            //Get-SOSAppDomain | Get-SOSAssembly - AppDomainSet
            //Get-SOSAppDomain | Get-SOSAssembly foo - AppDomainSet

            if (nameOrAddress != null)
            {
                bool haveLeft = false;
                bool haveRight = false;

                for (var i = 0; i < nameOrAddress.Length; i++)
                {
                    var item = nameOrAddress[i];

                    if (item.IsLeft)
                    {
                        var str = item.Left;

                        if (str.StartsWith("0x"))
                            str = str.Substring(2);

                        if (ulong.TryParse(str, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var number))
                        {
                            haveRight = true;
                            nameOrAddress[i] = new Either<string, CLRDATA_ADDRESS>(number);
                        }
                        else
                            haveLeft = true;
                    }
                    else
                        haveRight = true;
                }

                if (haveLeft && haveRight)
                    throw new ParameterBindingException("Cannot specify both an address and a name.");

                if (haveLeft)
                    ParameterSetName = ParameterSet.Default;
                else
                    ParameterSetName = ParameterSet.Address;
            }
        }

        protected abstract void ProcessRecordEx();
    }
}
