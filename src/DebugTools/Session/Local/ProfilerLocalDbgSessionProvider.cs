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

        public ProfilerLocalDbgSessionProvider() : base(DbgServiceType.Profiler, "Session")
        {
        }

        protected override ProfilerSession CreateServiceInternal(Process process, bool debugHost) => throw new NotSupportedException();

        public override void AddSpecial(ProfilerSession session)
        {
            specialSessions.Add(session);
        }

        public override void ReplaceSpecial(ProfilerSession oldService, ProfilerSession newService)
        {
            var index = specialSessions.IndexOf(oldService);

            if (index == -1)
                throw new InvalidOperationException($"Attempted to replace non-existent {nameof(ProfilerSession)} '{oldService}' with session '{newService}'. This should be impossible.");

            oldService.Dispose();
            specialSessions[index] = newService;
        }

        public override ProfilerSession GetOrCreateSpecial(object context)
        {
            var profilerContext = (CreateSpecialProfilerContext) context;

            if (profilerContext.Type != ProfilerSessionType.Global)
                throw new NotImplementedException("Only retrieving the global profiler service is currently supported");

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
                    throw new InvalidOperationException("Attempted to retrieve a the global profiler session, however one did not exist");
            }

            return global;
        }

        protected override bool TryCloseSpecial(ProfilerSession service)
        {
            var index = specialSessions.IndexOf(service);

            if (index != -1)
            {
                service.Dispose();
                specialSessions.RemoveAt(index);
                return true;
            }

            return false;
        }

        public void AddSpecial()
        {

        }

        protected override bool IsAlive(ProfilerSession service) =>
            service.IsAlive;

        protected override bool TryGetFallbackService(ProfilerSession[] dead, out ProfilerSession service)
        {
            if (dead.Length == 1)
            {
                service = dead[0];
                return true;
            }

            if (dead.Length > 0)
            {
                service = dead.Last(); //All of the sessions have ended, so take the last one
                return true;
            }

            //Special sessions are either file based or global. We don't want to allow implicitly returning file
            //sessions, so we only check for global

            var global = specialSessions.SingleOrDefault(s => s.Type == ProfilerSessionType.Global);

            if (global != null)
            {
                service = global;
                return true;
            }

            service = null;
            return false;
        }
    }
}
