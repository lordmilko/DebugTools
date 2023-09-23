using System.Runtime.InteropServices;

namespace DebugTools
{
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }
}