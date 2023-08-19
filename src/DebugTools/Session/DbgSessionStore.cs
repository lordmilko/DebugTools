using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DebugTools
{
    class DbgSessionStore
    {
        protected Dictionary<int, DbgSession> sessions = new Dictionary<int, DbgSession>();

        public DbgSession CreateDbgSession(Process process)
        {
            if (process.HasExited)
                throw new ArgumentException($"Cannot create {nameof(DbgSession)} for process {process.ProcessName} (PID: {process.Id}): process has exited");

            var pid = process.Id;

            var session = new DbgSession(pid);

            sessions.Add(pid, session);
            process.EnableRaisingEvents = true;
            process.Exited += (s, e) =>
            {
                if (sessions.TryGetValue(pid, out var val))
                    val.Dispose();

                sessions.Remove(pid);
            };

            return session;
        }

        public int[] GetServiceTargets() => sessions.Keys.ToArray();

        public T[] GetServices<T>(DbgServiceType type)
        {
            var results = new List<T>();

            foreach (var kv in sessions)
            {
                if (kv.Value.TryGetService(type, out var service))
                    results.Add((T) service);
            }

            return results.ToArray();
        }

        public T[] GetServices<T>() => sessions.SelectMany(s => s.Value.GetServices<T>()).ToArray();

        public bool TryGetValue(int processId, out DbgSession session) =>
            sessions.TryGetValue(processId, out session);

        public void DisposeService(DbgSessionHandle handle, DbgServiceType type)
        {
            if (sessions.TryGetValue(handle.ProcessId, out var session))
            {
                if (session.TryGetService(type, out var service))
                {
                    if (service is IDisposable d)
                        d.Dispose();
                }
            }
        }
    }
}
