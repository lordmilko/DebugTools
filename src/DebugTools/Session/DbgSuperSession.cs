using System;
using System.Collections.Generic;
using System.Linq;
using DebugTools.Profiler;

namespace DebugTools
{
    /// <summary>
    /// Encapsulates various debugging session types that may pertain to a given process (<see cref="ProfilerSession"/>, etc).<para/>
    /// The type of session type stored depends on whether this <see cref="DbgSuperSession"/> object is being created client side or server side.
    /// </summary>
    class DbgSuperSession : IDisposable
    {
        private Dictionary<DbgSessionType, object> subSessionMap = new Dictionary<DbgSessionType, object>();

        public int ProcessId { get; }

        private bool disposed;

        public DbgSuperSession(int processId)
        {
            ProcessId = processId;
        }

        public object this[DbgSessionType type]
        {
            set => subSessionMap.Add(type, value);
        }

        /// <summary>
        /// Gets whether this session container contains the specified session type.
        /// </summary>
        /// <param name="subSession">The type of session to check for.</param>
        /// <returns>True if this session container contains the specified session, otherwise false.</returns>
        public bool Contains(object subSession) =>
            subSessionMap.Values.Any(v => ReferenceEquals(v, subSession));

        public bool TryGetSubSession(DbgSessionType type, out object subSession) =>
            subSessionMap.TryGetValue(type, out subSession);

        public T[] GetSubSessions<T>() => subSessionMap.Values.OfType<T>().ToArray();

        /// <summary>
        /// Disposes the specified session (if applicable) and removes it from the list of subsessions
        /// stored in this <see cref="DbgSuperSession"/>.
        /// </summary>
        /// <param name="subSession">The session to close.</param>
        public void Close(object subSession)
        {
            foreach (var kv in subSessionMap)
            {
                if (ReferenceEquals(subSession, kv.Value))
                {
                    if (subSession is IDisposable d)
                        d.Dispose();

                    subSessionMap.Remove(kv.Key);

                    break;
                }
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            foreach (var kv in subSessionMap)
            {
                if (kv.Value is IDisposable d)
                    d.Dispose();
            }

            disposed = true;
        }
    }
}
