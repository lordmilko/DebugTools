using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Remoting;

namespace DebugTools.Host
{
    internal class RemoteExecutor : MarshalByRefObject
    {
        #region Local

        /// <summary>
        /// Marshals the <see cref="RemoteExecutor"/> from the remote process into the current process.
        /// </summary>
        /// <param name="process">The remote process containing the instance of <see cref="RemoteExecutor"/> to marshal.</param>
        /// <returns>The <see cref="RemoteExecutor"/> that was created in the remote process.</returns>
        public static RemoteExecutor GetInstanceFromRemoteProcess(Process process)
        {
            var uri = $"ipc://{GetPortName(process.Id)}/{typeof(RemoteExecutor).FullName}";

            return (RemoteExecutor)Activator.GetObject(typeof(RemoteExecutor), uri);
        }

        public T ExecuteInRemoteProcess<T>(Type type, string methodName)
        {
            var objectUri = Execute(type.Assembly.Location, type.FullName, methodName);

            if (objectUri == null)
                throw new InvalidOperationException($"Failed to execute method '{type.FullName}.{methodName}' in remote process. Verify the method is static and returns void.");

            return (T)Activator.GetObject(typeof(T), $"{BaseUri}/{objectUri}");
        }

        #endregion
        #region Remote

        private readonly ConcurrentDictionary<string, ObjRef> marshalledObjects = new ConcurrentDictionary<string, ObjRef>();

        public string PortName { get; }

        /// <summary>
        /// The base Uri of the service. This resolves to a string such as <c>ipc://IntegrationService_{HostProcessId}"</c>
        /// </summary>
        public string BaseUri { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteExecutor"/> class.<para/>
        /// This constructor is only called in the remote process.
        /// </summary>
        public RemoteExecutor()
        {
            PortName = GetPortName(Process.GetCurrentProcess().Id);
            BaseUri = "ipc://" + PortName;
        }

        /// <summary>
        /// Executes a static method that takes no arguments in the remote process, loading the assembly of the type the method belongs to in
        /// that process if it is not already loaded.
        /// </summary>
        /// <param name="assemblyFilePath">The path to the assembly the method's type belongs to.</param>
        /// <param name="typeFullName">The full name of the type the method belongs to.</param>
        /// <param name="methodName">The name of the method to execute.</param>
        /// <returns>A URI pointing to the type of object the method returned.</returns>
        public string Execute(string assemblyFilePath, string typeFullName, string methodName)
        {
            var assembly = Assembly.LoadFrom(assemblyFilePath);

            var type = assembly.GetType(typeFullName);
            var methodInfo = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            if (methodInfo == null)
                throw new InvalidOperationException($"Method '{typeFullName}.{methodName}()' does not exist");

            var result = methodInfo.Invoke(null, null);

            if (methodInfo.ReturnType == typeof(void))
                return null;

            // Create a unique URL for each object returned, so that we can communicate with each object individually
            var resultType = result.GetType();

            var marshallableResult = (MarshalByRefObject)result;
            var objectUri = $"{resultType.FullName}_{Guid.NewGuid()}";

            var marshalledObject = RemotingServices.Marshal(marshallableResult, objectUri, resultType);

            if (!marshalledObjects.TryAdd(objectUri, marshalledObject))
            {
                throw new InvalidOperationException($"An object with the specified URI has already been marshalled. (URI: {objectUri})");
            }

            return objectUri;
        }

        #endregion
        #region Shared

        private static string GetPortName(int remoteProcessId)
        {
            //Make the channel name well-known by using a static base and appending the process ID of the host.
            //Observe that the same name is generated in both the constructor in the remote process and in
            //GetInstanceFromRemoteProcess in the local process.
            return $"{nameof(RemoteExecutor)}_{{{remoteProcessId}}}";
        }

        #endregion

        /// <summary>
        /// Overrides the default lifetime behavior by ensuring this remote object does not get garbage collected after a period of time.
        /// </summary>
        /// <returns>This method always returns null.</returns>
        public override object InitializeLifetimeService() => null;
    }
}
