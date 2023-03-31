using System;
using System.Runtime.InteropServices;

namespace DebugTools.Profiler
{
    class ProfilerException : COMException
    {
        public new PROFILER_HRESULT HResult => (PROFILER_HRESULT) base.HResult;

        public ProfilerException(string message, Exception inner) : base(message, inner)
        {
        }

        public ProfilerException(PROFILER_HRESULT hr) : base(GetMessage(hr), (int) hr)
        {
        }

        public ProfilerException(string message, PROFILER_HRESULT hr) : base(message, (int) hr)
        {
        }

        private static string GetMessage(PROFILER_HRESULT hr)
        {
            switch (hr)
            {
                case PROFILER_HRESULT.PROFILER_E_STATICFIELD_DETAILED_REQUIRED:
                    return "Tracing static field values is only supported when the profiler is launched in Detailed Mode.";

                case PROFILER_HRESULT.PROFILER_E_STATICFIELD_INVALID_REQUEST:
                    return "The specified request to trace a static field was malformed.";

                case PROFILER_HRESULT.PROFILER_E_STATICFIELD_CLASS_NOT_FOUND:
                    return "A type with the specified name could not be found.";

                case PROFILER_HRESULT.PROFILER_E_STATICFIELD_CLASS_AMBIGUOUS:
                    return "Multiple types matching the specified name were found. Consider specifying a namespace + type to prevent ambiguities.";

                case PROFILER_HRESULT.PROFILER_E_STATICFIELD_FIELD_NOT_FOUND:
                    return "The specified field could not be found on the specified type.";

                case PROFILER_HRESULT.PROFILER_E_STATICFIELD_NOT_STATIC:
                    return "The specified field is not a static field";

                case PROFILER_HRESULT.PROFILER_E_STATICFIELD_FIELDTYPE_UNKNOWN:
                    return "The specified field is stored in memory in a way that is not known to DebugTools.";

                case PROFILER_HRESULT.PROFILER_E_STATICFIELD_FIELDTYPE_NOT_SUPPORTED:
                    return "The specified field was found, however the .NET runtime indicated the field is stored in memory in a way it does not support querying.";

                case PROFILER_HRESULT.PROFILER_E_STATICFIELD_MULTIPLE_APPDOMAIN:
                    return "Could not auto-detect the AppDomain to query the field from as multiple AppDomains were found in the profiled process. DebugTools currently only supports querying processes containing a single AppDomain.";

                case PROFILER_HRESULT.PROFILER_E_STATICFIELD_NEED_THREADID:
                    return "The specified field is thread-local however an OS Thread ID was not specified.";

                case PROFILER_HRESULT.PROFILER_E_STATICFIELD_THREAD_NOT_FOUND:
                    return "The specified field is thread-local however the specified OS Thread ID could not be found in the profiled process.";

                case PROFILER_HRESULT.PROFILER_E_STATICFIELD_INVALID_MEMORY:
                    return "A memory read error occurred while attempting to read the specified field. This can indicate a GC occurred while reading the field, causing memory to be moved.";

                case PROFILER_HRESULT.PROFILER_E_STATICFIELD_NOT_INITIALIZED:
                    return "The CLR reported that the field cannot be inspected as it, or its containing class, have not yet been initialized.";

                default:
                    return $"Error HRESULT {hr} has been returned from a call to a COM component.";
            }
        }
    }
}
