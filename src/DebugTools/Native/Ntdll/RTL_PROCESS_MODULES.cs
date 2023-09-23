using System;
using System.Runtime.InteropServices;

namespace DebugTools
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RTL_PROCESS_MODULES
    {
        public int NumberOfModules;

        public IntPtr Modules; //RTL_PROCESS_MODULE_INFORMATION[1]
    }
}
