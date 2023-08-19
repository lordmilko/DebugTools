using System;
using ClrDebug;

namespace DebugTools.Ui
{
    [Serializable]
    public class WindowMessage
    {
        [NonSerialized]
        private IUiElement window;

        public IUiElement Window
        {
            get => window;
            internal set => window = value;
        }

        public CORDB_ADDRESS hWnd { get; }

        public WM Message { get; }

        public CORDB_ADDRESS wParam { get; }

        public CORDB_ADDRESS lParam { get; }

        public WindowMessage(ulong hWnd, WM message, ulong wParam, ulong lParam)
        {
            this.hWnd = hWnd;
            Message = message;
            this.wParam = wParam;
            this.lParam = lParam;
        }

        protected internal static ulong LOWORD(ulong value) => value & 0xffff;
        protected internal static ulong HIWORD(ulong value) => (value >> 16) & 0xffff;

        protected internal static int GET_X_LPARAM(ulong value) => (short) LOWORD(value);

        protected internal static int GET_Y_LPARAM(ulong value) => (short) HIWORD(value);
    }
}
