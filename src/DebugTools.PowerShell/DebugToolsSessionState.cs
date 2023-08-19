namespace DebugTools.PowerShell
{
    internal static class DebugToolsSessionState
    {
        internal static LocalDbgSessionProviderFactory Services { get; } = new LocalDbgSessionProviderFactory();        
    }
}
