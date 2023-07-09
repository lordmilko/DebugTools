using System;
using System.Reflection;
using DebugTools.Dynamic;

namespace Profiler.Tests
{
    class DummyFactory : MarshalByRefObject
    {
        public RemoteProxyStub Create(string assemblyLocation, string typeName)
        {
            var assembly = Assembly.LoadFrom(assemblyLocation);

            var type = assembly.GetType(typeName);

            var instance = Activator.CreateInstance(type);

            return RemoteProxyStub.New(instance);
        }

        public override object InitializeLifetimeService() => null;
    }
}
