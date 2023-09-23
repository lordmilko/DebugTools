using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Serialization.Formatters;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Diagnostics.Runtime;
using DataTarget = Microsoft.Diagnostics.Runtime.DataTarget;

namespace DebugTools.Host
{
    public partial class HostApp : MarshalByRefObject
    {
        private RemoteDbgSessionProviderFactory sessionFactory = new RemoteDbgSessionProviderFactory();

        public void DisposeService(DbgSessionHandle handle, DbgServiceType type) => sessionFactory.DisposeService(handle, type);

        static HostApp()
        {
            var folder = DbgEngProvider.GetDebuggersPath(false);

            //ClrMD will try and load DbgHelp, so we must set the DLL directory before ClrMD is invoked
            if (folder != null)
                Kernel32.SetDllDirectory(folder);
        }

        public const string WaitForDebug = "DEBUGTOOLS_HOST_WAITFORDEBUG";
        public const string ParentPID = "DEBUGTOOLS_HOST_PARENT_PID";

        private static readonly BinaryServerFormatterSinkProvider DefaultSinkProvider = new BinaryServerFormatterSinkProvider()
        {
            TypeFilterLevel = TypeFilterLevel.Full
        };

        private static ObjRef marshalledRemoteExecutor;

        private static HostApp instance;

        public static HostApp GetInstance() => instance;

        #region Server

        public int ProcessId => Process.GetCurrentProcess().Id;

        public static void Main()
        {
            instance = new HostApp();

            Console.WriteLine("Start");

            WaitForDebugger();
            BindLifetimeToParentProcess();
            RunIPCServer();

            while (true)
                Thread.Sleep(1);
        }

        private static void WaitForDebugger()
        {
            var value = Environment.GetEnvironmentVariable(WaitForDebug);

            if (value == "1")
            {
                while (!Debugger.IsAttached)
                {
                    Console.WriteLine("Waiting for debugger...");
                    Thread.Sleep(100);
                }
            }
        }

        private static void BindLifetimeToParentProcess()
        {
            var str = Environment.GetEnvironmentVariable(ParentPID);

            if (!string.IsNullOrEmpty(str) && int.TryParse(str, out var val))
            {
                var parent = Process.GetProcessById(val);

                parent.EnableRaisingEvents = true;
                parent.Exited += (s, o) => Process.GetCurrentProcess().Kill();
            }
        }

        private static void RunIPCServer()
        {
            var friendlyName = AppDomain.CurrentDomain.FriendlyName;
            friendlyName = friendlyName.Substring(0, friendlyName.LastIndexOf("."));

            var remoteExecutor = new RemoteExecutor();

            //Stopping and starting the IPC Server while the client still has an active handle can
            //result in an access denied error when the client still has an active handle open to the named pipe.
            //By marking the server as exclusiveAddressUse = false, we succeed in recreating the server and it
            //is the client that blows up instead
            //https://social.msdn.microsoft.com/Forums/en-US/d154e4a9-3e31-41a5-944c-db867ca77e9e/ipcchannel-explicit-cleanup?forum=netfxremoting
            var ipcServer = new IpcServerChannel(
                new Dictionary<string, object>
                {
                    ["name"] = $"{friendlyName}.ServiceChannel_{Process.GetCurrentProcess().Id}",
                    ["portName"] = remoteExecutor.PortName,
                    ["exclusiveAddressUse"] = false
                },
                DefaultSinkProvider
            );

            var executorType = typeof(RemoteExecutor);
            marshalledRemoteExecutor = RemotingServices.Marshal(remoteExecutor, executorType.FullName, executorType);

            ipcServer.StartListening(null);

            var eventName = $"{friendlyName}_{Process.GetCurrentProcess().Id}";

            var waitHandle = new EventWaitHandle(true, EventResetMode.AutoReset, eventName);

            //After launching the server the client began waiting on an system wide event with the same name as this EventWaitHandle
            waitHandle.Set();
        }

        #endregion
        #region API

        public bool IsDebuggerAttached { get; set; }

        public DbgVtblSymbolInfo[] GetComObjects(int processId, string[] interfaces)
        {
            var process = Process.GetProcessById(processId);

            var regexes = interfaces?.Select(i => new Regex(i, RegexOptions.IgnoreCase | RegexOptions.Singleline)).ToArray();

            return WithClrMD(process, runtime =>
            {
                var rcws = runtime.Heap.EnumerateObjects().Where(o => o.Type?.Name == "System.__ComObject").ToArray();

                var results = new List<DbgVtblSymbolInfo>();

                var symbolManager = sessionFactory.GetOrCreateService<SymbolManager>(process);

                foreach (var rcw in rcws)
                {
                    if (rcw.Type.IsRCW(rcw))
                    {
                        var rcwData = rcw.Type.GetRCWData(rcw);

                        if (regexes != null)
                        {
                            if (!regexes.Any(r => rcwData.Interfaces.Any(i => r.IsMatch(DbgVtblSymbolInfo.CleanInterfaceName(i.Type.Name)))))
                                continue;
                        }

                        var symbol = symbolManager.GetVtblSymbol(rcwData);

                        if (symbol != null)
                            results.Add(symbol);
                    }
                }

                return results.ToArray();
            });
        }

        private T WithClrMD<T>(Process process, Func<ClrRuntime, T> func)
        {
            using (var dataTarget = DataTarget.AttachToProcess(process.Id, 0, AttachFlag.Passive))
            {
                var versions = dataTarget.ClrVersions;

                if (versions.Count == 0)
                    throw new InvalidOperationException($"Cannot attach ClrMD to process '{process.Id}': CLR runtime has not been initialized.");

                var runtime = versions[0].CreateRuntime();

                var result = func(runtime);

                return result;
            }
        }

        #endregion

        /// <summary>
        /// Overrides the default lifetime behavior by ensuring this remote object does not get garbage collected after a period of time.
        /// </summary>
        /// <returns>This method always returns null.</returns>
        public override object InitializeLifetimeService() => null;
    }
}
