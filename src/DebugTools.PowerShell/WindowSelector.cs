using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace DebugTools.PowerShell
{
    public partial class WindowSelector : Form
    {
        static WindowSelector()
        {
            Application.EnableVisualStyles();
        }

        public static IntPtr Execute()
        {
            var hWnd = Process.GetCurrentProcess().MainWindowHandle;
            var original = NativeMethods.GetThreadDpiAwarenessContext();

            if (original != NativeMethods.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2)
                NativeMethods.SetThreadDpiAwarenessContext(NativeMethods.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

            try
            {
                var selector = new WindowSelector(hWnd);

                var result = selector.ShowDialog();

                if (result == DialogResult.OK)
                    return selector.currentWindow;

                return IntPtr.Zero;
            }
            finally
            {
                if (original != NativeMethods.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2)
                    NativeMethods.SetThreadDpiAwarenessContext(original);
            }
        }

        private bool searching = false;
        private IntPtr currentWindow;
        private bool didDraw;
        private IntPtr parent;

        public WindowSelector(IntPtr parent)
        {
            this.parent = parent;

            InitializeComponent();

            img.Image = Resource.bullseyeWindow.ToBitmap();
        }

        private void img_MouseMove(object sender, MouseEventArgs e)
        {
            if (searching)
            {
                NativeMethods.GetCursorPos(out var cursorPos);
                var windowUnderMouse = NativeMethods.WindowFromPoint(cursorPos);

                if (currentWindow != windowUnderMouse)
                {
                    if (currentWindow != IntPtr.Zero && didDraw)
                        ApplyHighlight(currentWindow);

                    if (windowUnderMouse != IntPtr.Zero)
                    {
                        NativeMethods.GetWindowThreadProcessId(windowUnderMouse, out var pid);

                        if (pid != Process.GetCurrentProcess().Id && NativeMethods.IsWindow(windowUnderMouse))
                        {
                            ApplyHighlight(windowUnderMouse);
                            didDraw = true;
                        }
                        else
                            didDraw = false;
                    }

                    currentWindow = windowUnderMouse;
                }
            }
        }

        private void img_MouseUp(object sender, MouseEventArgs e)
        {
            if (searching)
            {
                searching = false;

                img.Image = Resource.bullseyeWindow.ToBitmap();
                Cursor.Current = DefaultCursor;

                NativeMethods.ReleaseCapture();

                if (currentWindow != IntPtr.Zero)
                {
                    if (didDraw)
                        ApplyHighlight(currentWindow);
                }

                NativeMethods.SetWindowPos(
                    parent,
                    NativeMethods.HWND_TOP,
                    0,
                    0,
                    0,
                    0,
                    SetWindowPosFlags.NOACTIVATE | SetWindowPosFlags.NOMOVE | SetWindowPosFlags.NOSIZE
                );

                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void img_MouseDown(object sender, MouseEventArgs e)
        {
            NativeMethods.SetCapture(img.Handle);

            img.Image = Resource.emptyWindow.ToBitmap();
            Cursor.Current = new Cursor(new MemoryStream(Resource.bullseye));
            searching = true;

            NativeMethods.SetWindowPos(
                Handle,
                NativeMethods.HWND_BOTTOM,
                0,
                0,
                0,
                0,
                SetWindowPosFlags.NOACTIVATE | SetWindowPosFlags.NOMOVE | SetWindowPosFlags.NOSIZE
            );

            NativeMethods.SetWindowPos(
                parent,
                NativeMethods.HWND_BOTTOM,
                0,
                0,
                0,
                0,
                SetWindowPosFlags.NOACTIVATE | SetWindowPosFlags.NOMOVE | SetWindowPosFlags.NOSIZE
            );
        }

        private void ApplyHighlight(IntPtr hWnd)
        {
            NativeMethods.GetWindowRect(hWnd, out var rect);
            var hdc = NativeMethods.GetWindowDC(hWnd);

            if (hdc != IntPtr.Zero)
            {
                var penWidth = NativeMethods.GetSystemMetrics(SystemMetric.SM_CXBORDER) * 3;
                var oldDC = NativeMethods.SaveDC(hdc);

                //Invert
                NativeMethods.SetROP2(hdc, NativeMethods.R2_NOT);

                var pen = NativeMethods.CreatePen(PenStyle.PS_INSIDEFRAME, penWidth, 0);
                NativeMethods.SelectObject(hdc, pen);

                var brush = NativeMethods.GetStockObject(StockObject.NULL_BRUSH);
                NativeMethods.SelectObject(hdc, brush);

                NativeMethods.Rectangle(hdc, 0, 0, rect.right - rect.left, rect.bottom - rect.top);

                NativeMethods.DeleteObject(pen);

                NativeMethods.RestoreDC(hdc, oldDC);
                NativeMethods.ReleaseDC(hWnd, hdc);
            }
        }
    }
}
