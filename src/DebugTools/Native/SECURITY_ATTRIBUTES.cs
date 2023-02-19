using System;
using System.Runtime.InteropServices;

namespace DebugTools
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct SECURITY_ATTRIBUTES
    {
        public int nLength;
        public IntPtr lpSecurityDescriptor;
        public bool bInheritHandle;
    }
}
