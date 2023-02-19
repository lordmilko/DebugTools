using System;

namespace DebugTools
{
    [Flags]
    public enum RtlQueryProcessFlag : uint
    {
        Modules = 0x00000001,
        Backtraces = 0x00000002,
        HeapSummary = 0x00000004,
        HeapTags = 0x00000008,
        HeapEntries = 0x00000010,
        Locks = 0x00000020,
        Modules32 = 0x00000040,
        NonInvasive = 0x80000000
    }
}
