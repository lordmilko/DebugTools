using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DebugTools.Host
{
    public class HostApp : MarshalByRefObject
    {
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

            Thread.Sleep(5000);

            Console.WriteLine("Env is " + value);

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

            waitHandle.Set();
        }

        #endregion

        /// <summary>
        /// Overrides the default lifetime behavior by ensuring this remote object does not get garbage collected after a period of time.
        /// </summary>
        /// <returns>This method always returns null.</returns>
        public override object InitializeLifetimeService() => null;
    }
}
