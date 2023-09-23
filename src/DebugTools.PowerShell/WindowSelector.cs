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
            var original = User32.GetThreadDpiAwarenessContext();

            if (original != User32.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2)
                User32.SetThreadDpiAwarenessContext(User32.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

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
                if (original != User32.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2)
                    User32.SetThreadDpiAwarenessContext(original);
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
                User32.GetCursorPos(out var cursorPos);
                var windowUnderMouse = User32.WindowFromPoint(cursorPos);

                if (currentWindow != windowUnderMouse)
                {
                    if (currentWindow != IntPtr.Zero && didDraw)
                        ApplyHighlight(currentWindow);

                    if (windowUnderMouse != IntPtr.Zero)
                    {
                        User32.GetWindowThreadProcessId(windowUnderMouse, out var pid);

                        if (pid != Process.GetCurrentProcess().Id && User32.IsWindow(windowUnderMouse))
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

                User32.ReleaseCapture();

                if (currentWindow != IntPtr.Zero)
                {
                    if (didDraw)
                        ApplyHighlight(currentWindow);
                }

                User32.SetWindowPos(
                    parent,
                    User32.HWND_TOP,
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
            User32.SetCapture(img.Handle);

            img.Image = Resource.emptyWindow.ToBitmap();
            Cursor.Current = new Cursor(new MemoryStream(Resource.bullseye));
            searching = true;

            User32.SetWindowPos(
                Handle,
                User32.HWND_BOTTOM,
                0,
                0,
                0,
                0,
                SetWindowPosFlags.NOACTIVATE | SetWindowPosFlags.NOMOVE | SetWindowPosFlags.NOSIZE
            );

            User32.SetWindowPos(
                parent,
                User32.HWND_BOTTOM,
                0,
                0,
                0,
                0,
                SetWindowPosFlags.NOACTIVATE | SetWindowPosFlags.NOMOVE | SetWindowPosFlags.NOSIZE
            );
        }

        private void ApplyHighlight(IntPtr hWnd)
        {
            User32.GetWindowRect(hWnd, out var rect);
            var hdc = User32.GetWindowDC(hWnd);

            if (hdc != IntPtr.Zero)
            {
                var penWidth = User32.GetSystemMetrics(SystemMetric.SM_CXBORDER) * 3;
                var oldDC = Gdi32.SaveDC(hdc);

                //Invert
                Gdi32.SetROP2(hdc, Gdi32.R2_NOT);

                var pen = Gdi32.CreatePen(PenStyle.PS_INSIDEFRAME, penWidth, 0);
                Gdi32.SelectObject(hdc, pen);

                var brush = Gdi32.GetStockObject(StockObject.NULL_BRUSH);
                Gdi32.SelectObject(hdc, brush);

                Gdi32.Rectangle(hdc, 0, 0, rect.right - rect.left, rect.bottom - rect.top);

                Gdi32.DeleteObject(pen);

                Gdi32.RestoreDC(hdc, oldDC);
                User32.ReleaseDC(hWnd, hdc);
            }
        }
    }
}
