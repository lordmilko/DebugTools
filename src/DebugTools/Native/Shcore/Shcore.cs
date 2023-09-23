using System;
using System.Runtime.InteropServices;
using ClrDebug;

namespace DebugTools
{
    static class Shcore
    {
        private const string shcore = "shcore.dll";

        [DllImport(shcore)]
        public static extern HRESULT GetProcessDpiAwareness(IntPtr hprocess, out PROCESS_DPI_AWARENESS value);

        [DllImport(shcore)]
        public static extern HRESULT SetProcessDpiAwareness(PROCESS_DPI_AWARENESS value);
    }
}
