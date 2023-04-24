namespace DebugTools.Profiler
{
    struct MMFEventHeader
    {
        public long QPC;
        public int ThreadId;
        public int UserDataSize;
        public ushort EventType;
    }
}