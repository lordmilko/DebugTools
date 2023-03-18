using ClrDebug;

namespace DebugTools.Profiler
{
    static class HRESULTExtensions
    {
        public static bool IsProfilerHRESULT(this HRESULT hr)
        {
            return (uint) hr >= 0x80041001 && (uint) hr <= 0x80042000;
        }
    }

    /// <summary>
    /// Specifies custom HRESULT values that can be returned from the profiler.
    /// </summary>
    public enum PROFILER_HRESULT : uint
    {
        /// <summary>
        /// The profiler attempted to read more parameter or return value data than the communication buffer supports.
        /// </summary>
        PROFILER_E_BUFFERFULL = 0x80041001,

        /// <summary>
        /// Failed to resolve the ClassID of what is most likely a generic type.
        /// </summary>
        PROFILER_E_GENERICCLASSID = 0x80041002,

        /// <summary>
        /// An array T[] was encountered of a type the profiler has not been notified of.
        /// </summary>
        PROFILER_E_UNKNOWN_GENERIC_ARRAY = 0x80041003,

        /// <summary>
        /// Could not get the ClassID of a type from the runtime or any fallback strategy.
        /// </summary>
        PROFILER_E_NO_CLASSID = 0x80041004,

        /// <summary>
        /// Attempted to resolve an mdTypeRef from a resolution scope not currently supported by the profiler.
        /// </summary>
        PROFILER_E_UNKNOWN_RESOLUTION_SCOPE = 0x80041005,

        /// <summary>
        /// The Module pertaining to a given ModuleID was not found in the cache.
        /// </summary>
        PROFILER_E_MISSING_MODULE = 0x80041006,

        /// <summary>
        /// Encountered an unexpected frame upon attempting to leave a function, indicating that the shadow stack trace is corrupt.
        /// </summary>
        PROFILER_E_UNKNOWN_FRAME = 0x80041007,

        /// <summary>
        /// The method pertaining to a given FunctionID was not found in the cache.
        /// </summary>
        PROFILER_E_UNKNOWN_METHOD = 0x80041008,

        #region Static Field

        /// <summary>
        /// Detailed profiling must be enabled in order to read static field values.
        /// </summary>
        PROFILER_E_STATICFIELD_DETAILED_REQUIRED = 0x80041009,

        /// <summary>
        /// The string indicating the type and field to be traced did not contain a dot.
        /// </summary>
        PROFILER_E_STATICFIELD_INVALID_REQUEST = 0x8004100A,

        /// <summary>
        /// A class or struct matching the type to be traced could not be found.
        /// </summary>
        PROFILER_E_STATICFIELD_CLASS_NOT_FOUND = 0x8004100B,

        /// <summary>
        /// Multiple types were found that matched the specified field's parent type. Consider specifying a fully qualified type, including namespace.
        /// </summary>
        PROFILER_E_STATICFIELD_CLASS_AMBIGUOUS = 0x8004100C,

        /// <summary>
        /// A field matching the field to be traced could not be found.
        /// </summary>
        PROFILER_E_STATICFIELD_FIELD_NOT_FOUND = 0x8004100D,

        /// <summary>
        /// The field that was requested to be traced is not a static field.
        /// </summary>
        PROFILER_E_STATICFIELD_NOT_STATIC = 0x8004100E,

        /// <summary>
        /// The field that was requested to be traced was of a field type that DebugTools does not know how to parse.
        /// </summary>
        PROFILER_E_STATICFIELD_FIELDTYPE_UNKNOWN = 0x8004100F,

        /// <summary>
        /// The field that was requested to be traced was of a field type that the .NET runtime does not support querying.
        /// </summary>
        PROFILER_E_STATICFIELD_FIELDTYPE_NOT_SUPPORTED = 0x80041010,

        /// <summary>
        /// Could not auto-detect the appdomain of the field to be traced as multiple appdomains are currently loaded.
        /// </summary>
        PROFILER_E_STATICFIELD_MULTIPLE_APPDOMAIN = 0x80041011,

        /// <summary>
        /// The static field to be traced is a thread local field but no thread ID was specified.
        /// </summary>
        PROFILER_E_STATICFIELD_NEED_THREADID = 0x80041012,

        /// <summary>
        /// The OS thread of the field to be trace could not be found in the profiled process.
        /// </summary>
        PROFILER_E_STATICFIELD_THREAD_NOT_FOUND = 0x80041013,

        /// <summary>
        /// A memory read error occurred while attempting to trace the specified field, indicating that a GC may have occurred and an object was moved.
        /// </summary>
        PROFILER_E_STATICFIELD_INVALID_MEMORY = 0x80041014,

        #endregion
    }
}
