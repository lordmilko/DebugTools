using System;
using System.Diagnostics;

namespace DebugTools
{
    abstract class RemoteDbgSessionProvider<T> : DbgSessionProvider
    {
        public abstract DbgServiceType ServiceType { get; }

        public T GetOrCreateService(Process process)
        {
            object service;

            if (Store.TryGetValue(process.Id, out var session))
            {
                if (session.TryGetService(ServiceType, out service))
                    return (T)service;
            }

            if (session == null)
                session = Store.CreateDbgSession(process);

            service = CreateServiceInternal(process);

            session[ServiceType] = service;

            return (T)service;
        }

        public DbgSessionHandle CreateService(int processId, bool lazy = false, bool debugTarget = false)
        {
            var process = Process.GetProcessById(processId);

            if (!Store.TryGetValue(process.Id, out var session))
                session = Store.CreateDbgSession(process);
            else
            {
                if (session.TryGetService(ServiceType, out _))
                    throw new InvalidOperationException($"Cannot create {typeof(T).Name}: an instance for process ID {processId} already exists.");
            }

            object service;

            if (lazy)
            {
                service = new Lazy<T>(() =>
                {
                    var result = CreateServiceInternal(process);

                    if (debugTarget)
                        throw new NotImplementedException("Debugging a lazily created service has not been tested");

                    return result;
                });
            }
            else
            {
                service = CreateServiceInternal(process);

                if (debugTarget)
                    VsDebugger.Attach(process);
            }

            session[ServiceType] = service;

            return new DbgSessionHandle(process.Id);
        }

        public T GetService(int processId)
        {
            if (Store.TryGetValue(processId, out var session))
            {
                if (session.TryGetService(ServiceType, out var service))
                {
                    if (service is Lazy<T> l)
                        return l.Value;

                    return (T) service;
                }
            }

            throw new InvalidOperationException($"Failed to find an existing {typeof(T).Name} for process ID {processId}");
        }

        protected abstract T CreateServiceInternal(Process process);
    }
}
