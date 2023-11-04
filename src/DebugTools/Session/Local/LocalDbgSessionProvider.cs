using System;
using System.Diagnostics;
using System.Linq;

namespace DebugTools
{
    abstract class LocalDbgSessionProvider<T> : DbgSessionProvider
    {
        public DbgSessionType SessionType { get; }
        private string parameter;

        protected LocalDbgSessionProvider(DbgSessionType sessionType, string parameter)
        {
            SessionType = sessionType;
            this.parameter = parameter;
        }

        internal new LocalDbgSessionStore Store => (LocalDbgSessionStore) base.Store;

        public T Create(Process process, bool debugHost)
        {
            if (!TryCreate(process, debugHost, out var subSession))
                throw new InvalidOperationException($"Cannot create {typeof(T).Name}: an instance for process '{process.Id}' already exists.");

            return subSession;
        }

        public bool TryCreate(Process process, bool debugHost, out T subSession)
        {
            if (Store.TryGetValue(process.Id, out var superSession))
            {
                if (superSession.TryGetSubSession(SessionType, out var existing))
                {
                    //Can't create subsession, one already exists
                    subSession = (T) existing;
                    return false;
                }
            }

            if (superSession == null)
                superSession = Store.CreateDbgSuperSession(process);

            subSession = CreateSubSessionInternal(process, debugHost);

            superSession[SessionType] = subSession;
            return true;
        }

        public bool TryAdd(int processId, T subSession)
        {
            var process = Process.GetProcessById(processId);

            if (Store.TryGetValue(process.Id, out var superSession))
            {
                if (superSession.TryGetSubSession(SessionType, out _))
                {
                    //Can't add subsession, one already exists
                    return false;
                }
            }

            if (superSession == null)
                superSession = Store.CreateDbgSuperSession(process);

            superSession[SessionType] = subSession;
            return true;
        }

        public T GetImplicitSubSession(bool mandatory = true)
        {
            var candidates = Store.GetSubSessions<T>(SessionType);

            //Check for alive

            var alive = candidates.Where(IsAlive).ToArray();

            if (alive.Length == 1)
                return alive[0];

            if (alive.Length > 1)
                throw new InvalidOperationException($"Cannot execute cmdlet: no -{parameter} was specified and more than one {parameter} belonging to active processes was found in the PowerShell session.");

            //Check for dead

            if (TryGetFallbackSubSession(candidates, out var subSession))
                return subSession;

            if (mandatory)
                throw new InvalidOperationException($"Cannot execute cmdlet: no -{parameter} was specified and no global {parameter} could be found in the PowerShell session.");

            return default;
        }

        protected abstract T CreateSubSessionInternal(Process process, bool debugHost);

        public virtual void AddSpecial(T subSession) => throw new NotSupportedException();
        public virtual void ReplaceSpecial(T oldSubSession, T newSubSession) => throw new NotSupportedException();
        public virtual T GetOrCreateSpecial(object context) => throw new NotSupportedException();
        protected virtual bool TryCloseSpecial(T subSession) => false;

        public void Close(int processId, T subSession)
        {
            if (!TryCloseSpecial(subSession))
                Store.Close(processId, SessionType, subSession);
        }

        protected abstract bool IsAlive(T subSession);

        protected virtual bool TryGetFallbackSubSession(T[] dead, out T subSession)
        {
            if (dead.Length > 0)
                throw new InvalidOperationException($"Cannot execute cmdlet: no -Process was specified and all previous {typeof(T).Name} instances have now terminated.");

            subSession = default;
            return false;
        }

        internal virtual bool IsValidFallback(int? pid) => true;
    }
}
