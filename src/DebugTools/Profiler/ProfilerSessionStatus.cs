namespace DebugTools.Profiler
{
    public enum ProfilerSessionStatus
    {
        /// <summary>
        /// Indicates that the session is currently active, and if there is a process targeted by this session, that it is currently running.
        /// </summary>
        Active,

        /// <summary>
        /// Indicates that the process targeted by this session is no longer running.
        /// </summary>
        Exited,

        /// <summary>
        /// Indicates that this session was stopped by Stop-DbgProfiler.
        /// </summary>
        Terminated
    }
}
