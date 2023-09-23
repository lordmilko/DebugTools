using System;
using System.Runtime.InteropServices;

namespace DebugTools
{
    static class Gdi32
    {
        private const string gdi32 = "gdi32.dll";

        internal const int R2_NOT = 6;

        [DllImport(gdi32, SetLastError = true)]
        public static extern IntPtr CreatePen(PenStyle iStyle, int cWidth, uint color);

        [DllImport(gdi32, SetLastError = true)]
        public static extern bool DeleteObject(IntPtr ho);

        [DllImport(gdi32, SetLastError = true)]
        public static extern IntPtr GetStockObject(StockObject i);

        [DllImport(gdi32, SetLastError = true)]
        public static extern bool Rectangle(IntPtr hdc, int left, int top, int right, int bottom);

        [DllImport(gdi32, SetLastError = true)]
        public static extern bool RestoreDC(IntPtr hdc, int nSavedDC);

        [DllImport(gdi32, SetLastError = true)]
        public static extern int SaveDC(IntPtr hdc);

        [DllImport(gdi32, SetLastError = true)]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

        [DllImport(gdi32, SetLastError = true)]
        public static extern int SetROP2(IntPtr hdc, int rop2);
    }
}
