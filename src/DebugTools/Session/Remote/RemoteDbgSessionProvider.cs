using System;
using System.Diagnostics;
using ChaosLib;

namespace DebugTools
{
    abstract class RemoteDbgSessionProvider<T> : DbgSessionProvider
    {
        public abstract DbgSessionType SessionType { get; }

        public T GetOrCreateSubSession(Process process)
        {
            object subSession;

            if (Store.TryGetValue(process.Id, out var superSession))
            {
                if (superSession.TryGetSubSession(SessionType, out subSession))
                    return (T)subSession;
            }

            if (superSession == null)
                superSession = Store.CreateDbgSuperSession(process);

            subSession = CreateSubSessionInternal(process);

            superSession[SessionType] = subSession;

            return (T)subSession;
        }

        public DbgSessionHandle CreateSubSession(int processId, bool lazy = false, bool debugTarget = false)
        {
            var process = Process.GetProcessById(processId);

            if (!Store.TryGetValue(process.Id, out var superSession))
                superSession = Store.CreateDbgSuperSession(process);
            else
            {
                if (superSession.TryGetSubSession(SessionType, out _))
                    throw new InvalidOperationException($"Cannot create {typeof(T).Name}: an instance for process ID {processId} already exists.");
            }

            object subSession;

            if (lazy)
            {
                subSession = new Lazy<T>(() =>
                {
                    var result = CreateSubSessionInternal(process);

                    if (debugTarget)
                        throw new NotImplementedException("Debugging a lazily created subsession has not been tested");

                    return result;
                });
            }
            else
            {
                subSession = CreateSubSessionInternal(process);

                if (debugTarget)
                    VsDebugger.Attach(process, VsDebuggerType.Native);
            }

            superSession[SessionType] = subSession;

            return new DbgSessionHandle(process.Id);
        }

        public T GetSubSession(int processId)
        {
            if (Store.TryGetValue(processId, out var superSession))
            {
                if (superSession.TryGetSubSession(SessionType, out var subSession))
                {
                    if (subSession is Lazy<T> l)
                        return l.Value;

                    return (T) subSession;
                }
            }

            throw new InvalidOperationException($"Failed to find an existing {typeof(T).Name} for process ID {processId}");
        }

        protected abstract T CreateSubSessionInternal(Process process);
    }
}
