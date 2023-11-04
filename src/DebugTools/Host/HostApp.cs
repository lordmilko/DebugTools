using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using ChaosLib;
using ClrDebug;
using DebugTools.Dynamic;
using DebugTools.SOS;
using Microsoft.Diagnostics.Runtime;
using DataTarget = Microsoft.Diagnostics.Runtime.DataTarget;

namespace DebugTools.Host
{
    public partial class HostApp : MarshalByRefObject
    {
        private RemoteDbgSessionProviderFactory sessionFactory = new RemoteDbgSessionProviderFactory();

        public void DisposeSubSession(DbgSessionHandle handle, DbgSessionType type) => sessionFactory.DisposeSubSession(handle, type);

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

        private static RemoteExecutor remoteExecutor;
        private static ObjRef marshalledRemoteExecutor;

        private static HostApp instance;

        public static HostApp GetInstance() => instance;

        private static CancellationTokenSource cts;

        #region Server

        public int ProcessId => Process.GetCurrentProcess().Id;

        public static void Main(bool standalone = true)
        {
            cts = new CancellationTokenSource();

            instance = new HostApp();

            if (standalone)
                Console.WriteLine("Start");

            WaitForDebugger();
            BindLifetimeToParentProcess();
            RunIPCServer();

            while (!cts.IsCancellationRequested)
                Thread.Sleep(1);
        }

        public static int MainNative(string args)
        {
            if (args.Contains("-debug"))
            {
                while (!Debugger.IsAttached)
                {
                    Console.WriteLine("Waiting for debugger...");
                    Thread.Sleep(100);
                }
            }

            Main(false);
            return 0;
        }

        public void Exit() => cts?.Cancel();

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
            var friendlyName = $"DebugTools.Host.{(IntPtr.Size == 4 ? "x86" : "x64")}";

            remoteExecutor = new RemoteExecutor();

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

        public object CreateRemoteStub(StaticFieldInfo fieldInfo)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                //Can't touch Location in a dynamic assembly
                if (assembly.IsDynamic)
                    continue;

                if (fieldInfo.AssemblyPath.Equals(assembly.Location, StringComparison.OrdinalIgnoreCase))
                {
                    var type = assembly.GetType(fieldInfo.DeclaringType);

                    if (type == null)
                        throw new InvalidOperationException($"Cannot find type '{fieldInfo.DeclaringType}' in assembly '{assembly.Location}'");

                    FieldInfo field = null;

                    while (field == null && type != null && type != typeof(object))
                    {
                        field = type.GetField(fieldInfo.InternalName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                        if (field == null)
                            type = type.BaseType;
                    }

                    if (field == null)
                        throw new InvalidOperationException($"Cannot find field '{fieldInfo.InternalName}' on type '{fieldInfo.DeclaringType}' or its ancestors.");

                    var value = field.GetValue(null);

                    var result = RemoteProxyStub.Wrap(value, remoteExecutor);

                    return result;
                }
            }

            throw new InvalidOperationException($"Failed to find assembly for field '{fieldInfo}'.");
        }

        public StaticFieldsResult GetStaticFields(int processId, string assemblyRegexPattern, string typeRegexPattern, string fieldRegexPattern)
        {
            var process = Process.GetProcessById(processId);

            var assemblyRegex = WildcardRegexOrDefault(assemblyRegexPattern);
            var typeRegex = WildcardRegexOrDefault(typeRegexPattern);
            var fieldRegex = WildcardRegexOrDefault(fieldRegexPattern);

            bool matchAssembly(SOSAssembly a) => assemblyRegex == null || (a.Name != null && assemblyRegex.IsMatch(Path.GetFileName(a.Name)));
            bool matchMethodTable(string name)
            {
                if (typeRegexPattern == null)
                    return true;

                if (typeRegex.IsMatch(name))
                    return true;

                if (typeRegexPattern.StartsWith("^") && typeRegexPattern.EndsWith("$") && !typeRegexPattern.Contains("."))
                {
                    //We're trying to match the type name without namespace

                    var dot = name.LastIndexOf('.');

                    if (dot != -1 && dot < name.Length - 1)
                    {
                        var nameWithoutNS = name.Substring(dot + 1);

                        if (typeRegex.IsMatch(nameWithoutNS))
                            return true;
                    }
                }

                return false;
            }

            string getMethodTableName(string name)
            {
                var tilde = name.IndexOf('`');

                if (tilde != -1)
                    name = name.Substring(0, tilde);

                return name;
            }

            var warnings = new List<string>();
            var results = new List<StaticFieldInfo>();

            var sosProcess = new SOSProcess(process);

            var items = (from appDomain in SOSAppDomain.GetAppDomains(sosProcess.SOS)
                      from assembly in SOSAssembly.GetAssemblies(appDomain, sosProcess.SOS)
                      where matchAssembly(assembly)
                      from module in SOSModule.GetModules(assembly, sosProcess.SOS)
                      from methodTable in SOSMethodTable.GetMethodTables(module, sosProcess.SOS)
                      let methodTableName = getMethodTableName(methodTable.Name)
                      where matchMethodTable(methodTableName)
                      select new
                      {
                          Assembly = assembly,
                          Name = methodTableName,
                          MethodTable = methodTable
                      }).ToArray();

            foreach (var item in items)
            {
                var staticFields = SOSFieldDesc.GetFieldDescs(item.MethodTable, sosProcess.SOS).Where(f => f.bIsStatic).ToArray();

                var matchingFields = staticFields.Select(f => new StaticFieldInfo(item.Assembly, item.Name, f, sosProcess)).Where(v =>
                {
                    if (fieldRegex == null)
                        return true;

                    //Match the property or field name rather than the backing field name (in the case of a property)
                    return fieldRegex.IsMatch(v.Name);
                }).ToArray();

                if (matchingFields.Length > 0)
                {
                    var rawMethodTable = sosProcess.DataTarget.ReadVirtual<MethodTable>(item.MethodTable.Address);

                    if (rawMethodTable.HasInstantiation)
                    {
                        foreach (var field in matchingFields)
                        {
                            var name = item.MethodTable.Name;

                            if (item.MethodTable.Name.Contains("`"))
                            {
                                //Construct a nice generic name. Foo<T> or Foo<T0, T1>.
                                //I don't know if the names of nested types inside a generic type can also be generic, so we construct
                                //the name manually, rather than using a dumb regex replace

                                var builder = new StringBuilder();
                                var chars = name.ToCharArray();

                                for (var i = 0; i < chars.Length; i++)
                                {
                                    if (chars[i] == '`')
                                    {
                                        var numChars = new List<char>();

                                        for (var j = i + 1; j < chars.Length; i++, j++)
                                            numChars.Add(chars[j]);

                                        var num = Convert.ToInt32(new string(numChars.ToArray()));

                                        builder.Append("<");

                                        for (var j = 0; j < num; j++)
                                        {
                                            builder.Append("T");

                                            if (num != 1)
                                                builder.Append(j);

                                            if (j < num - 1)
                                                builder.Append(", ");
                                        }

                                        builder.Append(">");
                                    }
                                    else
                                        builder.Append(chars[i]);
                                }

                                name = builder.ToString();
                            }

                            warnings.Add($"{name}.{field.Name}");
                        }
                    }
                    else
                        results.AddRange(matchingFields);
                }
            }

            return new StaticFieldsResult(results.ToArray(), warnings.ToArray());
        }

        public DbgVtblSymbolInfo[] GetComObjects(int processId, string[] interfaces)
        {
            var process = Process.GetProcessById(processId);

            var regexes = interfaces?.Select(WildcardRegexOrDefault).ToArray();

            return WithClrMD(process, runtime =>
            {
                var rcws = runtime.Heap.EnumerateObjects().Where(o => o.Type?.Name == "System.__ComObject").ToArray();

                var results = new List<DbgVtblSymbolInfo>();

                var symbolManager = sessionFactory.GetOrCreateSubSession<SymbolManager>(process);

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

        private Regex WildcardRegexOrDefault(string pattern)
        {
            if (pattern == null)
                return null;

            return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }

        #endregion

        /// <summary>
        /// Overrides the default lifetime behavior by ensuring this remote object does not get garbage collected after a period of time.
        /// </summary>
        /// <returns>This method always returns null.</returns>
        public override object InitializeLifetimeService() => null;
    }
}
