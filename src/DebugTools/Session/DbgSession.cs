using System;
using System.Collections.Generic;
using System.Linq;

namespace DebugTools
{
    class DbgSession : IDisposable
    {
        private Dictionary<DbgServiceType, object> services = new Dictionary<DbgServiceType, object>();

        public int ProcessId { get; }

        private bool disposed;

        public DbgSession(int processId)
        {
            ProcessId = processId;
        }

        public object this[DbgServiceType type]
        {
            set => services.Add(type, value);
        }

        public bool Contains(object service) =>
            services.Values.Any(v => ReferenceEquals(v, service));

        public bool TryGetService(DbgServiceType type, out object service) =>
            services.TryGetValue(type, out service);

        public T[] GetServices<T>() => services.Values.OfType<T>().ToArray();

        public void Close(object service)
        {
            foreach (var kv in services)
            {
                if (ReferenceEquals(service, kv.Value))
                {
                    if (service is IDisposable d)
                        d.Dispose();

                    services.Remove(kv.Key);

                    break;
                }
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            foreach (var kv in services)
            {
                if (kv.Value is IDisposable d)
                    d.Dispose();
            }

            disposed = true;
        }
    }
}
