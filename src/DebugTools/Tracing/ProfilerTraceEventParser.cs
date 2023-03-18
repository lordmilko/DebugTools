using System;
using Microsoft.Diagnostics.Tracing;

namespace DebugTools.Tracing
{
    public class ProfilerTraceEventParser : TraceEventParser
    {
        static class EventId
        {
            public const int CallEnter = 1;
            public const int CallExit = 2;
            public const int Tailcall = 3;

            public const int CallEnterDetailed = 4;
            public const int CallExitDetailed = 5;
            public const int TailcallDetailed = 6;

            public const int ManagedToUnmanaged = 7;
            public const int UnmanagedToManaged = 8;

            public const int Exception = 9;
            public const int ExceptionFrameUnwind = 10;
            public const int ExceptionCompleted = 11;

            public const int StaticFieldValue = 12;

            public const int MethodInfo = 13;
            public const int MethodInfoDetailed = 14;

            public const int ModuleLoaded = 15;

            public const int ThreadCreate = 16;
            public const int ThreadDestroy = 17;
            public const int ThreadName = 18;

            public const int Shutdown = 19;
        }

        /// <summary>
        /// Gets the name of the provider. This value matches the &lt;provider name/&gt; specified in DebugToolsProvider.man
        /// </summary>
        public static string ProviderName = "DebugToolsProvider";

        /// <summary>
        /// Gets the GUID of the provider. This value matches the &lt;provider guid/&gt; specified in DebugToolsProvider.man
        /// </summary>
        public static Guid ProviderGuid = new Guid("{C6F30827-DD2D-4FEE-AD2E-BBA0CE6CBD8F}");

        private static TraceEvent[] events;

        #region Events

        public event Action<CallArgs> CallEnter
        {
            add => source.RegisterEventTemplate(CallEnterTemplate(value));
            remove => source.UnregisterEventTemplate(value, EventId.CallEnter, ProviderGuid);
        }

        public event Action<CallArgs> CallLeave
        {
            add => source.RegisterEventTemplate(CallLeaveTemplate(value));
            remove => source.UnregisterEventTemplate(value, EventId.CallExit, ProviderGuid);
        }

        public event Action<CallArgs> Tailcall
        {
            add => source.RegisterEventTemplate(TailcallTemplate(value));
            remove => source.UnregisterEventTemplate(value, EventId.Tailcall, ProviderGuid);
        }

        public event Action<CallDetailedArgs> CallEnterDetailed
        {
            add => source.RegisterEventTemplate(CallEnterDetailedTemplate(value));
            remove => source.UnregisterEventTemplate(value, EventId.CallEnterDetailed, ProviderGuid);
        }

        public event Action<CallDetailedArgs> CallLeaveDetailed
        {
            add => source.RegisterEventTemplate(CallLeaveDetailedTemplate(value));
            remove => source.UnregisterEventTemplate(value, EventId.CallExitDetailed, ProviderGuid);
        }

        public event Action<CallDetailedArgs> TailcallDetailed
        {
            add => source.RegisterEventTemplate(TailcallDetailedTemplate(value));
            remove => source.UnregisterEventTemplate(value, EventId.TailcallDetailed, ProviderGuid);
        }

        public event Action<UnmanagedTransitionArgs> ManagedToUnmanaged
        {
            add => source.RegisterEventTemplate(ManagedToUnmanagedTemplate(value));
            remove => source.UnregisterEventTemplate(value, EventId.ManagedToUnmanaged, ProviderGuid);
        }

        public event Action<UnmanagedTransitionArgs> UnmanagedToManaged
        {
            add => source.RegisterEventTemplate(UnmanagedToManagedTemplate(value));
            remove => source.UnregisterEventTemplate(value, EventId.UnmanagedToManaged, ProviderGuid);
        }

        public event Action<ExceptionArgs> Exception
        {
            add => source.RegisterEventTemplate(ExceptionTemplate(value));
            remove => source.UnregisterEventTemplate(value, EventId.Exception, ProviderGuid);
        }

        public event Action<CallArgs> ExceptionFrameUnwind
        {
            add => source.RegisterEventTemplate(ExceptionFrameUnwindTemplate(value));
            remove => source.UnregisterEventTemplate(value, EventId.ExceptionFrameUnwind, ProviderGuid);
        }

        public event Action<ExceptionCompletedArgs> ExceptionCompleted
        {
            add => source.RegisterEventTemplate(ExceptionCompletedTemplate(value));
            remove => source.UnregisterEventTemplate(value, EventId.ExceptionCompleted, ProviderGuid);
        }

        public event Action<StaticFieldValueArgs> StaticFieldValue
        {
            add => source.RegisterEventTemplate(StaticFieldValueTemplate(value));
            remove => source.UnregisterEventTemplate(value, EventId.StaticFieldValue, ProviderGuid);
        }

        public event Action<MethodInfoArgs> MethodInfo
        {
            add => source.RegisterEventTemplate(MethodInfoTemplate(value));
            remove => source.UnregisterEventTemplate(value, EventId.MethodInfo, ProviderGuid);
        }

        public event Action<MethodInfoDetailedArgs> MethodInfoDetailed
        {
            add => source.RegisterEventTemplate(MethodInfoDetailedTemplate(value));
            remove => source.UnregisterEventTemplate(value, EventId.MethodInfoDetailed, ProviderGuid);
        }

        public event Action<ModuleLoadedArgs> ModuleLoaded
        {
            add => source.RegisterEventTemplate(ModuleLoadedTemplate(value));
            remove => source.UnregisterEventTemplate(value, EventId.ModuleLoaded, ProviderGuid);
        }

        public event Action<ThreadArgs> ThreadCreate
        {
            add => source.RegisterEventTemplate(ThreadCreateTemplate(value));
            remove => source.UnregisterEventTemplate(value, EventId.ThreadCreate, ProviderGuid);
        }
        
        public event Action<ThreadArgs> ThreadDestroy
        {
            add => source.RegisterEventTemplate(ThreadDestroyTemplate(value));
            remove => source.UnregisterEventTemplate(value, EventId.ThreadDestroy, ProviderGuid);
        }

        public event Action<ThreadNameArgs> ThreadName
        {
            add => source.RegisterEventTemplate(ThreadNameTemplate(value));
            remove => source.UnregisterEventTemplate(value, EventId.ThreadName, ProviderGuid);
        }

        public event Action<ShutdownArgs> Shutdown
        {
            add => source.RegisterEventTemplate(ShutdownTemplate(value));
            remove => source.UnregisterEventTemplate(value, EventId.Shutdown, ProviderGuid);
        }

        #endregion

        public ProfilerTraceEventParser(TraceEventSource source, bool dontRegister = false) : base(source, dontRegister)
        {
        }

        protected override string GetProviderName() => ProviderName;

        protected override void EnumerateTemplates(Func<string, string, EventFilterResponse> eventsToObserve, Action<TraceEvent> callback)
        {
            if (events == null)
            {
                events = new TraceEvent[]
                {
                    CallEnterTemplate(null),
                    CallLeaveTemplate(null),
                    TailcallTemplate(null),

                    CallEnterDetailedTemplate(null),
                    CallLeaveDetailedTemplate(null),
                    TailcallDetailedTemplate(null),

                    ManagedToUnmanagedTemplate(null),
                    UnmanagedToManagedTemplate(null),

                    ExceptionTemplate(null),
                    ExceptionFrameUnwindTemplate(null),
                    ExceptionCompletedTemplate(null),

                    StaticFieldValueTemplate(null),

                    MethodInfoTemplate(null),
                    MethodInfoDetailedTemplate(null),

                    ModuleLoadedTemplate(null),

                    ThreadCreateTemplate(null),
                    ThreadDestroyTemplate(null),
                    ThreadNameTemplate(null),

                    ShutdownTemplate(null)
                };
            }

            foreach (var item in events)
            {
                if (eventsToObserve == null || eventsToObserve(item.ProviderName, item.EventName) == EventFilterResponse.AcceptEvent)
                    callback(item);
            }
        }

        #region Create Templates

        private static CallArgs CallEnterTemplate(Action<CallArgs> action) => new CallArgs(action, EventId.CallEnter, 0, null, Guid.Empty, 0, null, ProviderGuid, ProviderName);

        private static CallArgs CallLeaveTemplate(Action<CallArgs> action) => new CallArgs(action, EventId.CallExit, 0, null, Guid.Empty, 0, null, ProviderGuid, ProviderName);

        private static CallArgs TailcallTemplate(Action<CallArgs> action) => new CallArgs(action, EventId.Tailcall, 0, null, Guid.Empty, 0, null, ProviderGuid, ProviderName);

        private static CallDetailedArgs CallEnterDetailedTemplate(Action<CallDetailedArgs> action) => new CallDetailedArgs(action, EventId.CallEnterDetailed, 0, null, Guid.Empty, 0, null, ProviderGuid, ProviderName);

        private static CallDetailedArgs CallLeaveDetailedTemplate(Action<CallDetailedArgs> action) => new CallDetailedArgs(action, EventId.CallExitDetailed, 0, null, Guid.Empty, 0, null, ProviderGuid, ProviderName);

        private static CallDetailedArgs TailcallDetailedTemplate(Action<CallDetailedArgs> action) => new CallDetailedArgs(action, EventId.TailcallDetailed, 0, null, Guid.Empty, 0, null, ProviderGuid, ProviderName);

        private static UnmanagedTransitionArgs ManagedToUnmanagedTemplate(Action<UnmanagedTransitionArgs> action) => new UnmanagedTransitionArgs(action, EventId.ManagedToUnmanaged, 0, null, Guid.Empty, 0, null, ProviderGuid, ProviderName);

        private static UnmanagedTransitionArgs UnmanagedToManagedTemplate(Action<UnmanagedTransitionArgs> action) => new UnmanagedTransitionArgs(action, EventId.UnmanagedToManaged, 0, null, Guid.Empty, 0, null, ProviderGuid, ProviderName);

        private static ExceptionArgs ExceptionTemplate(Action<ExceptionArgs> action) => new ExceptionArgs(action, EventId.Exception, 0, null, Guid.Empty, 0, null, ProviderGuid, ProviderName);

        private static CallArgs ExceptionFrameUnwindTemplate(Action<CallArgs> action) => new CallArgs(action, EventId.ExceptionFrameUnwind, 0, null, Guid.Empty, 0, null, ProviderGuid, ProviderName);

        private static ExceptionCompletedArgs ExceptionCompletedTemplate(Action<ExceptionCompletedArgs> action) => new ExceptionCompletedArgs(action, EventId.ExceptionCompleted, 0, null, Guid.Empty, 0, null, ProviderGuid, ProviderName);

        private static StaticFieldValueArgs StaticFieldValueTemplate(Action<StaticFieldValueArgs> action) => new StaticFieldValueArgs(action, EventId.StaticFieldValue, 0, null, Guid.Empty, 0, null, ProviderGuid, ProviderName);

        private static MethodInfoArgs MethodInfoTemplate(Action<MethodInfoArgs> action) => new MethodInfoArgs(action, EventId.MethodInfo, 0, null, Guid.Empty, 0, null, ProviderGuid, ProviderName);

        private static MethodInfoDetailedArgs MethodInfoDetailedTemplate(Action<MethodInfoDetailedArgs> action) => new MethodInfoDetailedArgs(action, EventId.MethodInfoDetailed, 0, null, Guid.Empty, 0, null, ProviderGuid, ProviderName);

        private static ModuleLoadedArgs ModuleLoadedTemplate(Action<ModuleLoadedArgs> action) => new ModuleLoadedArgs(action, EventId.ModuleLoaded, 0, null, Guid.Empty, 0, null, ProviderGuid, ProviderName);

        private static ThreadArgs ThreadCreateTemplate(Action<ThreadArgs> action) => new ThreadArgs(action, EventId.ThreadCreate, 0, null, Guid.Empty, 0, null, ProviderGuid, ProviderName);

        private static ThreadArgs ThreadDestroyTemplate(Action<ThreadArgs> action) => new ThreadArgs(action, EventId.ThreadDestroy, 0, null, Guid.Empty, 0, null, ProviderGuid, ProviderName);

        private static ThreadNameArgs ThreadNameTemplate(Action<ThreadNameArgs> action) => new ThreadNameArgs(action, EventId.ThreadName, 0, null, Guid.Empty, 0, null, ProviderGuid, ProviderName);

        public static ShutdownArgs ShutdownTemplate(Action<ShutdownArgs> action) => new ShutdownArgs(action, EventId.Shutdown, 0, null, Guid.Empty, 0, null, ProviderGuid, ProviderName);

        #endregion
    }
}
