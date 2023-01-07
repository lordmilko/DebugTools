namespace DebugTools.Profiler
{
    public interface IRootFrame : IFrame
    {
        int ThreadId { get; set; }

        string ThreadName { get; set; }
    }
}
