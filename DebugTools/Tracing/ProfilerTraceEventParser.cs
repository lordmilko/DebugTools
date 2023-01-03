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

            public const int MethodInfo = 7;
            public const int MethodInfoDetailed = 8;

            public const int ThreadCreate = 9;
            public const int ThreadDestroy = 10;
            public const int ThreadName = 11;

            public const int Shutdown = 12;
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
                var arr = new TraceEvent[12];
                arr[0] = CallEnterTemplate(null);
                arr[1] = CallLeaveTemplate(null);
                arr[2] = TailcallTemplate(null);

                arr[3] = CallEnterDetailedTemplate(null);
                arr[4] = CallLeaveDetailedTemplate(null);
                arr[5] = TailcallDetailedTemplate(null);

                arr[6] = MethodInfoTemplate(null);
                arr[7] = MethodInfoDetailedTemplate(null);

                arr[8] = ThreadCreateTemplate(null);
                arr[9] = ThreadDestroyTemplate(null);
                arr[10] = ThreadNameTemplate(null);

                arr[11] = ShutdownTemplate(null);
                events = arr;
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

        private static MethodInfoArgs MethodInfoTemplate(Action<MethodInfoArgs> action) => new MethodInfoArgs(action, EventId.MethodInfo, 0, null, Guid.Empty, 0, null, ProviderGuid, ProviderName);

        private static MethodInfoDetailedArgs MethodInfoDetailedTemplate(Action<MethodInfoDetailedArgs> action) => new MethodInfoDetailedArgs(action, EventId.MethodInfoDetailed, 0, null, Guid.Empty, 0, null, ProviderGuid, ProviderName);

        private static ThreadArgs ThreadCreateTemplate(Action<ThreadArgs> action) => new ThreadArgs(action, EventId.ThreadCreate, 0, null, Guid.Empty, 0, null, ProviderGuid, ProviderName);

        private static ThreadArgs ThreadDestroyTemplate(Action<ThreadArgs> action) => new ThreadArgs(action, EventId.ThreadDestroy, 0, null, Guid.Empty, 0, null, ProviderGuid, ProviderName);

        private static ThreadNameArgs ThreadNameTemplate(Action<ThreadNameArgs> action) => new ThreadNameArgs(action, EventId.ThreadName, 0, null, Guid.Empty, 0, null, ProviderGuid, ProviderName);

        public static ShutdownArgs ShutdownTemplate(Action<ShutdownArgs> action) => new ShutdownArgs(action, EventId.Shutdown, 0, null, Guid.Empty, 0, null, ProviderGuid, ProviderName);

        #endregion
    }
}
