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

        public T GetOrCreateSubSession<T>(Process process)
        {
            var provider = GetProvider<T>();

            var result = provider.GetOrCreateSubSession(process);

            return result;
        }

        public DbgSessionHandle CreateSubSession<T>(int processId, bool lazy = false, bool debugTarget = false)
        {
            var provider = GetProvider<T>();

            var result = provider.CreateSubSession(processId, lazy, debugTarget);

            return result;
        }

        public T GetSubSession<T>(int processId)
        {
            var provider = GetProvider<T>();

            var result = provider.GetSubSession(processId);

            return result;
        }

        public void DisposeSubSession(DbgSessionHandle handle, DbgSessionType type) =>
            store.DisposeSubSession(handle, type);

        private RemoteDbgSessionProvider<T> GetProvider<T>()
        {
            if (providers.TryGetValue(typeof(T), out var provider))
                return (RemoteDbgSessionProvider<T>) provider;

            throw new NotImplementedException($"Cannot retrieve remote provider for type {typeof(T).Name}: no provider has been registered");
        }
    }
}
