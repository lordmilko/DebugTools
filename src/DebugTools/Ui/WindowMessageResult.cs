namespace DebugTools.Ui
{
    class WindowMessageResult : WindowMessage
    {
        public ulong Result { get; }

        public WindowMessageResult(ulong hWnd, WM message, ulong wParam, ulong lParam, ulong result) : base(hWnd, message, wParam, lParam)
        {
            Result = result;
        }
    }
}
