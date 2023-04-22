namespace DebugTools.Profiler
{
    public enum ProfilerEnvFlags
    {
        WaitForDebugger,
        Detailed,
        TraceStart,
        TraceValueDepth,
        TargetProcess,
        ModuleBlacklist,
        ModuleWhitelist,
        IgnorePointerValue,
        IgnoreDefaultBlacklist,
        SynchronousTransfers,

        DisablePipe,
        IncludeUnknownUnmanagedTransitions,
        Minimized
    }
}
