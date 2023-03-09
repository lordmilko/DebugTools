namespace DebugTools.Profiler
{
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
        PROFILER_E_UNKNOWN_METHOD = 0x80041008
    }
}
