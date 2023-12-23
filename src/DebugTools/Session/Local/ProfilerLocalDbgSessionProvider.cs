using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DebugTools.Profiler;

namespace DebugTools
{
    public struct CreateSpecialProfilerContext
    {
        public ProfilerSessionType Type { get; }
        public bool Mandatory { get; }
        public bool MayCreate { get; }

        public CreateSpecialProfilerContext(ProfilerSessionType type, bool mandatory, bool mayCreate)
        {
            Type = type;
            Mandatory = mandatory;
            MayCreate = mayCreate;
        }
    }

    class ProfilerLocalDbgSessionProvider : LocalDbgSessionProvider<ProfilerSession>
    {
        private List<ProfilerSession> specialSessions = new List<ProfilerSession>();

        public ProfilerLocalDbgSessionProvider() : base(DbgSessionType.Profiler, "Session")
        {
        }

        protected override ProfilerSession CreateSubSessionInternal(Process process, bool debugHost) => throw new NotSupportedException();

        public override void AddSpecial(ProfilerSession session)
        {
            specialSessions.Add(session);
        }

        public override void ReplaceSpecial(ProfilerSession oldSubSession, ProfilerSession newSubSession)
        {
            var index = specialSessions.IndexOf(oldSubSession);

            if (index == -1)
                throw new InvalidOperationException($"Attempted to replace non-existent {nameof(ProfilerSession)} '{oldSubSession}' with session '{newSubSession}'. This should be impossible.");

            oldSubSession.Dispose();
            specialSessions[index] = newSubSession;
        }

        public override ProfilerSession GetOrCreateSpecial(object context)
        {
            var profilerContext = (CreateSpecialProfilerContext) context;

            if (profilerContext.Type != ProfilerSessionType.Global)
                throw new NotImplementedException("Only retrieving the global profiler session is currently supported");

            var global = specialSessions.SingleOrDefault(v => v.Type == profilerContext.Type);

            if (global != null)
                return global;

            if (profilerContext.MayCreate)
            {
                global = new ProfilerSession(new LiveProfilerReaderConfig(ProfilerSessionType.Global, null));

                global.StartGlobal();
            }
            else
            {
                if (profilerContext.Mandatory)
                    throw new InvalidOperationException("Attempted to retrieve the global profiler session, however one did not exist");
            }

            return global;
        }

        protected override bool TryCloseSpecial(ProfilerSession subSession)
        {
            var index = specialSessions.IndexOf(subSession);

            if (index != -1)
            {
                subSession.Dispose();
                specialSessions.RemoveAt(index);
                return true;
            }

            return false;
        }

        public void AddSpecial()
        {

        }

        protected override bool IsAlive(ProfilerSession subSession) =>
            subSession.IsAlive;

        protected override bool TryGetFallbackSubSession(ProfilerSession[] dead, out ProfilerSession subSession)
        {
            if (dead.Length == 1)
            {
                subSession = dead[0];
                return true;
            }

            if (dead.Length > 0)
            {
                subSession = dead.Last(); //All of the sessions have ended, so take the last one
                return true;
            }

            //Special sessions are either file based or global. First, prefer global if it exists

            var global = specialSessions.SingleOrDefault(s => s.Type == ProfilerSessionType.Global);

            if (global != null)
            {
                subSession = global;
                return true;
            }

            //No global, we'll take anything we can get

            var special = specialSessions.FirstOrDefault();

            if (special != null)
            {
                subSession = special;
                return true;
            }

            subSession = null;
            return false;
        }
    }
}
