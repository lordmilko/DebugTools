using System;

namespace DebugTools
{
    [Serializable]
    public struct DbgSessionHandle
    {
        public int ProcessId { get; }

        public DbgSessionHandle(int processId)
        {
            ProcessId = processId;
        }
    }
}
