using System;
using System.Collections.Generic;
using System.Linq;
using DebugTools.Profiler;

namespace DebugTools
{
    /// <summary>
    /// Encapsulates various debugging session/service types that may pertain to a given process (<see cref="ProfilerSession"/>, etc).<para/>
    /// The type of session/service type stored depends on whether this <see cref="DbgSession"/> object is being created client side or server side.
    /// </summary>
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

        /// <summary>
        /// Gets whether this session container contains the specified session/service type.
        /// </summary>
        /// <param name="service">The type of session/service to check for.</param>
        /// <returns>True if this session container contains the specified session/service, otherwise false.</returns>
        public bool Contains(object service) =>
            services.Values.Any(v => ReferenceEquals(v, service));

        public bool TryGetService(DbgServiceType type, out object service) =>
            services.TryGetValue(type, out service);

        public T[] GetServices<T>() => services.Values.OfType<T>().ToArray();

        /// <summary>
        /// Disposes the specified service/session (if applicable) and removes it from the list of services
        /// stored in this <see cref="DbgSession"/>.
        /// </summary>
        /// <param name="service">The service/session to close.</param>
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
