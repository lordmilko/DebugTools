using System;
using System.Collections.Generic;
using System.Diagnostics;
using DebugTools.SOS;
using DebugTools.Ui;

namespace DebugTools
{
    class RemoteDbgSessionProviderFactory
    {
        private Dictionary<Type, DbgSessionProvider> providers;
        private DbgSessionStore store;

        public RemoteDbgSessionProviderFactory()
        {
            providers = new Dictionary<Type, DbgSessionProvider>
            {
                { typeof(SOSProcess), new SOSRemoteDbgSessionProvider() },
                { typeof(UiMessageSession), new UiMessageRemoteDbgSessionProvider() }
            };

            store = new DbgSessionStore();

            foreach (var kv in providers)
                kv.Value.Store = store;
        }

        public T GetOrCreateService<T>(Process process)
        {
            var provider = GetProvider<T>();

            var result = provider.GetOrCreateService(process);

            return result;
        }

        public DbgSessionHandle CreateService<T>(int processId, bool lazy = false, bool debugTarget = false)
        {
            var provider = GetProvider<T>();

            var result = provider.CreateService(processId, lazy, debugTarget);

            return result;
        }

        public T GetService<T>(int processId)
        {
            var provider = GetProvider<T>();

            var result = provider.GetService(processId);

            return result;
        }

        public void DisposeService(DbgSessionHandle handle, DbgServiceType type) =>
            store.DisposeService(handle, type);

        private RemoteDbgSessionProvider<T> GetProvider<T>()
        {
            if (providers.TryGetValue(typeof(T), out var provider))
                return (RemoteDbgSessionProvider<T>) provider;

            throw new NotImplementedException($"Cannot retrieve remote provider for type {typeof(T).Name}: no provider has been registered");
        }
    }
}
