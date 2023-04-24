using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using DebugTools.Tracing;
using Microsoft.Diagnostics.Tracing;

namespace DebugTools.Profiler
{
    class FakeTraceEventProvider
    {
        static TraceEvent[] events = {
            null,
            new CallArgs(null, 0, 0, null, default, 0, null, default, null), //CallEnter 1
            new CallArgs(null, 0, 0, null, default, 0, null, default, null), //CallExit 2
            new CallArgs(null, 0, 0, null, default, 0, null, default, null), //Tailcall 3
            new CallDetailedArgs(null, 0, 0, null, default, 0, null, default, null), //CallEnterDetailed 4
            new CallDetailedArgs(null, 0, 0, null, default, 0, null, default, null), //CallExitDetailed 5
            new CallDetailedArgs(null, 0, 0, null, default, 0, null, default, null), //TailcallDetailed 6
            new UnmanagedTransitionArgs(null, 0, 0, null, default, 0, null, default, null), //ManagedToUnmanaged 7
            new UnmanagedTransitionArgs(null, 0, 0, null, default, 0, null, default, null), //UnmanagedToManaged 8
            new ExceptionArgs(null, 0, 0, null, default, 0, null, default, null), //Exception 9
            new CallArgs(null, 0, 0, null, default, 0, null, default, null), //ExceptionFrameUnwind 10
            new ExceptionCompletedArgs(null, 0, 0, null, default, 0, null, default, null), //ExceptionCompleted 11
            new StaticFieldValueArgs(null, 0, 0, null, default, 0, null, default, null), //StaticFieldValue 12
            new MethodInfoArgs(null, 0, 0, null, default, 0, null, default, null), //MethodInfo 13
            new MethodInfoDetailedArgs(null, 0, 0, null, default, 0, null, default, null), //MethodInfoDetailed 14
            new ModuleLoadedArgs(null, 0, 0, null, default, 0, null, default, null), //ModuleLoaded 15
            new ThreadArgs(null, 0, 0, null, default, 0, null, default, null), //ThreadCreate 16
            new ThreadArgs(null, 0, 0, null, default, 0, null, default, null), //ThreadDestroy 17
            new ThreadNameArgs(null, 0, 0, null, default, 0, null, default, null), //ThreadName 18
            new ShutdownArgs(null, 0, 0, null, default, 0, null, default, null) //Shutdown 19
        };

        private static IntPtr eventRecordBuffer;
        private static int threadIdOffset;
        private static int timeStampOffset;
        private static int userDataLengthOffset;
        private static FieldInfo userDataField;

        static FakeTraceEventProvider()
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;

            userDataField = typeof(TraceEvent).GetField("userData", flags);
            var eventRecordField = typeof(TraceEvent).GetField("eventRecord", flags);
            var traceEventSourceField = typeof(TraceEvent).GetField("traceEventSource", flags);

            var eventRecordType = eventRecordField.FieldType.GetElementType();
            var eventRecordSize = Marshal.SizeOf(eventRecordType);
            var eventHeaderOffset = (int) Marshal.OffsetOf(eventRecordType, "EventHeader");
            userDataLengthOffset = (int) Marshal.OffsetOf(eventRecordType, "UserDataLength");

            var eventHeaderType = eventRecordType.GetField("EventHeader").FieldType;

            threadIdOffset = eventHeaderOffset + (int)Marshal.OffsetOf(eventHeaderType, "ThreadId");
            timeStampOffset = eventHeaderOffset + (int)Marshal.OffsetOf(eventHeaderType, "TimeStamp");

            //As this is static this is never unallocated
            eventRecordBuffer = Marshal.AllocHGlobal(eventRecordSize);

            var source = (ETWTraceEventSource)FormatterServices.GetUninitializedObject(typeof(ETWTraceEventSource));
            source.GetType().GetProperty("IsRealTime").GetSetMethod(true).Invoke(source, new object[] { true });
            source.GetType().GetField("_QPCFreq", flags).SetValue(source, Stopwatch.Frequency);
            source.SynchronizeClock();

            foreach (var item in events.Skip(1))
            {
                eventRecordField.SetValue(item, eventRecordBuffer);
                traceEventSourceField.SetValue(item, source);
            }
        }

        public static unsafe TraceEvent GetEvent(
            ref MMFEventHeader header,
            byte* blobPtr)
        {
            var data = events[header.EventType];

            Marshal.WriteInt32(eventRecordBuffer, threadIdOffset, header.ThreadId);
            Marshal.WriteInt64(eventRecordBuffer, timeStampOffset, header.QPC);
            Marshal.WriteInt32(eventRecordBuffer, userDataLengthOffset, header.UserDataSize);
            userDataField.SetValue(data, new IntPtr(blobPtr));

            return data;
        }
    }
}
