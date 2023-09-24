using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DebugTools.Host;
using DebugTools.Profiler;
using DebugTools.Ui;

namespace DebugTools
{
    class LocalDbgSessionProviderFactory
    {
        private Dictionary<Type, DbgSessionProvider> providers;

        private LocalDbgSessionStore store;

        public LocalDbgSessionProviderFactory()
        {
            providers = new Dictionary<Type, DbgSessionProvider>
            {
                { typeof(LocalSOSProcess), new SOSLocalDbgSessionProvider() },
                { typeof(ProfilerSession), new ProfilerLocalDbgSessionProvider() },
                { typeof(LocalUiSession), new UiLocalDbgSessionProvider() }
            };

            store = new LocalDbgSessionStore();

            foreach (var kv in providers)
                kv.Value.Store = store;
        }

        public T[] GetSubSessions<T>() => store.GetSubSessions<T>();

        public T Create<T>(Process process, bool debugHost)
        {
            var provider = GetProvider<T>();

            return provider.Create(process, debugHost);
        }

        public bool TryCreate<T>(Process process, bool debugHost, out T service)
        {
            var provider = GetProvider<T>();

            return provider.TryCreate(process, debugHost, out service);
        }

        public void Add<T>(int processId, T service)
        {
            var provider = GetProvider<T>();

            if (!provider.TryAdd(processId, service))
                throw new InvalidOperationException($"Cannot add service '{service}' a service with this type already exists for process ID {processId}");
        }

        public void AddSpecial<T>(T service)
        {
            var provider = GetProvider<T>();

            provider.AddSpecial(service);
        }

        public void ReplaceSpecial<T>(T oldService, T newService)
        {
            var provider = GetProvider<T>();

            provider.ReplaceSpecial(oldService, newService);
        }

        public T GetOrCreateSpecial<T>(object context)
        {
            var provider = GetProvider<T>();

            return provider.GetOrCreateSpecial(context);
        }

        public T GetImplicitSubSession<T>(bool mandatory = true)
        {
            var provider = GetProvider<T>();

            return provider.GetImplicitSubSession(mandatory);
        }

        public void Close<T>(int processId, T service)
        {
            var provider = GetProvider<T>();

            provider.Close(processId, service);
        }

        public HostApp GetDetectedHost(Process process, bool needDebug)
        {
            var pid = process.Id;

            var info = store.GetDetectedHostInfo(process, needDebug);

            process.EnableRaisingEvents = true;
            process.Exited += (s, e) =>
            {
                //We've given out a HostApp for a target process that we may or may not
                //have a proper session for now or in the future. When the target process ends,
                //ostensibly we want to release the reference count on the HostApp. We can't just
                //do that however, because we may actually have a proper service with the same process ID
                //that is responsible for releasing the reference count on the HostApp, not us

                var targets = store.GetSessionTargets();

                if (!targets.Contains(pid))
                {
                    info.Release(pid);
                }
            };

            return info.Host;
        }

        public Process GetImplicitOrFallbackProcess()
        {
            var targets = store.GetSessionTargets();

            var processes = Process.GetProcesses().ToDictionary(p => p.Id);

            foreach (var target in targets)
            {
                if (processes.TryGetValue(target, out var process))
                    return process;
            }

            throw new InvalidOperationException("Could not identify the process to implicitly use, and no explicit process was specified.");
        }

        public T GetImplicitOrFallbackService<T>()
        {
            var provider = GetProvider<T>();

            var services = store.GetSubSessions<T>(provider.SessionType);            

            if (services.Length == 0)
            {
                //We don't have any instances of the desired service. Have we created any other types of services?
                //If so we can fallback to using that

                var pid = store.GetSessionTargets().Cast<int?>().FirstOrDefault();

                if (pid != null)
                {
                    var process = Process.GetProcessById(pid.Value);

                    //We already checked above there were no instances of type T, so it's implied we must be creating the first T here
                    if (!provider.TryCreate(process, false, out var service))
                        throw new InvalidOperationException($"Failed to register {typeof(T).Name} for a process because an instance already existed. This should be impossible");

                    return service;
                }
            }

            //We have at least one instance of the specified service type, or we failed to find a fallback service to use
            //above. Use the normal service resolution logic, which will either find a service to use, or throw an appropriate
            //exception
            return provider.GetImplicitSubSession();
        }

        private LocalDbgSessionProvider<T> GetProvider<T>()
        {
            if (providers.TryGetValue(typeof(T), out var provider))
                return (LocalDbgSessionProvider<T>) provider;

            throw new NotImplementedException($"Cannot retrieve remote provider for type {typeof(T).Name}: no provider has been registered");
        }
    }
}
