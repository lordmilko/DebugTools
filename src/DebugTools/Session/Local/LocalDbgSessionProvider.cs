using System;
using System.Diagnostics;
using System.Linq;

namespace DebugTools
{
    abstract class LocalDbgSessionProvider<T> : DbgSessionProvider
    {
        public DbgServiceType ServiceType { get; }
        private string parameter;

        protected LocalDbgSessionProvider(DbgServiceType serviceType, string parameter)
        {
            ServiceType = serviceType;
            this.parameter = parameter;
        }

        internal new LocalDbgSessionStore Store => (LocalDbgSessionStore) base.Store;

        public T Create(Process process, bool debugHost)
        {
            if (!TryCreate(process, debugHost, out var service))
                throw new InvalidOperationException($"Cannot create {typeof(T).Name}: an instance for process '{process.Id}' already exists.");

            return service;
        }

        public bool TryCreate(Process process, bool debugHost, out T service)
        {
            if (Store.TryGetValue(process.Id, out var session))
            {
                if (session.TryGetService(ServiceType, out var existing))
                {
                    //Can't create service, one already exists
                    service = (T) existing;
                    return false;
                }
            }

            if (session == null)
                session = Store.CreateDbgSession(process);

            service = CreateServiceInternal(process, debugHost);

            session[ServiceType] = service;
            return true;
        }

        public bool TryAdd(int processId, T service)
        {
            var process = Process.GetProcessById(processId);

            if (Store.TryGetValue(process.Id, out var session))
            {
                if (session.TryGetService(ServiceType, out _))
                {
                    //Can't add service, one already exists
                    return false;
                }
            }

            if (session == null)
                session = Store.CreateDbgSession(process);

            session[ServiceType] = service;
            return true;
        }

        public T GetImplicitService(bool mandatory = true)
        {
            var candidates = Store.GetServices<T>(ServiceType);

            //Check for alive

            var alive = candidates.Where(IsAlive).ToArray();

            if (alive.Length == 1)
                return alive[0];

            if (alive.Length > 1)
                throw new InvalidOperationException($"Cannot execute cmdlet: no -{parameter} was specified and more than one {parameter} belonging to active processes was found in the PowerShell session.");

            //Check for dead

            if (TryGetFallbackService(candidates, out var service))
                return service;

            if (mandatory)
                throw new InvalidOperationException($"Cannot execute cmdlet: no -{parameter} was specified and no global {parameter} could be found in the PowerShell session.");

            return default;
        }

        protected abstract T CreateServiceInternal(Process process, bool debugHost);

        public virtual void AddSpecial(T service) => throw new NotSupportedException();
        public virtual void ReplaceSpecial(T oldService, T newService) => throw new NotSupportedException();
        public virtual T GetOrCreateSpecial(object context) => throw new NotSupportedException();
        protected virtual bool TryCloseSpecial(T service) => false;

        public void Close(int processId, T service)
        {
            if (!TryCloseSpecial(service))
                Store.Close(processId, ServiceType, service);
        }

        protected abstract bool IsAlive(T service);

        protected virtual bool TryGetFallbackService(T[] dead, out T service)
        {
            if (dead.Length > 0)
                throw new InvalidOperationException($"Cannot execute cmdlet: no -Process was specified and all previous {typeof(T).Name} instances have now terminated.");

            service = default;
            return false;
        }

        
    }
}
