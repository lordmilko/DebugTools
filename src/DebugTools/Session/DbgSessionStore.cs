using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DebugTools
{
    class DbgSessionStore
    {
        protected Dictionary<int, DbgSuperSession> superSessions = new Dictionary<int, DbgSuperSession>();

        public DbgSuperSession CreateDbgSuperSession(Process process)
        {
            if (process.HasExited)
                throw new ArgumentException($"Cannot create {nameof(DbgSuperSession)} for process {process.ProcessName} (PID: {process.Id}): process has exited");

            var pid = process.Id;

            var superSession = new DbgSuperSession(pid);

            superSessions.Add(pid, superSession);
            process.EnableRaisingEvents = true;
            process.Exited += (s, e) =>
            {
                //Dispose the session, but don't remove it. If we've just been profiling a process, we may still
                //want to analyze our collected results!

                if (superSessions.TryGetValue(pid, out var val))
                    val.Dispose();
            };

            return superSession;
        }

        public int[] GetSessionTargets() => superSessions.Keys.ToArray();

        public T[] GetSubSessions<T>(DbgSessionType type)
        {
            var results = new List<T>();

            foreach (var kv in superSessions)
            {
                if (kv.Value.TryGetSubSession(type, out var subSession))
                    results.Add((T) subSession);
            }

            return results.ToArray();
        }

        public T[] GetSubSessions<T>() => superSessions.SelectMany(s => s.Value.GetSubSessions<T>()).ToArray();

        public bool TryGetValue(int processId, out DbgSuperSession session) =>
            superSessions.TryGetValue(processId, out session);

        public void DisposeSubSession(DbgSessionHandle handle, DbgSessionType type)
        {
            if (superSessions.TryGetValue(handle.ProcessId, out var superSession))
            {
                if (superSession.TryGetSubSession(type, out var subSession))
                {
                    if (subSession is IDisposable d)
                        d.Dispose();
                }
            }
        }
    }
}
