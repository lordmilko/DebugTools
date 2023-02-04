using System;
using System.Runtime.InteropServices;

namespace DebugTools
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct RTL_DEBUG_INFORMATION
    {
        public IntPtr SectionHandleClient;
        public IntPtr ViewBaseClient;
        public IntPtr ViewBaseTarget;
        public IntPtr ViewBaseDelta;
        public IntPtr EventPairClient;
        public IntPtr EventPairTarget;
        public IntPtr TargetProcessId;
        public IntPtr TargetThreadHandle;
        public uint Flags;
        public IntPtr OffsetFree;
        public IntPtr CommitSize;
        public IntPtr ViewSize;

        public RTL_PROCESS_MODULES* Modules;

        //PRTL_PROCESS_BACKTRACES
        public IntPtr BackTraces;

        //PRTL_PROCESS_HEAPS
        public IntPtr Heaps;

        ///PRTL_PROCESS_LOCKS
        public IntPtr Locks;

        public IntPtr SpecificHeap;
        public IntPtr TargetProcessHandle;

        //Ideally we'd like to have a fixed array here, but we can't because you can't use IntPtr/nuint in a fixed array.
        //And if we use a byval array, we can't have a pointer to RTL_DEBUG_INFORMATION

        public IntPtr Reserved_1;
        public IntPtr Reserved_2;
        public IntPtr Reserved_3;
        public IntPtr Reserved_4;
        public IntPtr Reserved_5;
        public IntPtr Reserved_6;
    }
}
