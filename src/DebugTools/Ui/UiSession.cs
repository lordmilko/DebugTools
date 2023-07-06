using System;
using System.Diagnostics;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

namespace DebugTools.Ui
{
    public class UiSession : IDisposable
    {
        public Process Process { get; }

        private IUiElement root;

        public IUiElement Root
        {
            get
            {
                if (((AutomationElement) root).IsOffscreen)
                {
                    var enumerator = new MainWindowEnumerator();

                    //Maybe the process has a login page which is the real main window while the other one is offscreen,
                    //and it's now changed? For some reason we'll just get the same main window handle if we try and do this via Process.GetProcessById again
                    if (enumerator.TryGetMainWindows(Process.Id, out var match) && match != Process.MainWindowHandle)
                        return UiElement.New(automation.FromHandle(match));
                }

                return root;
            }
        }

        private readonly Application app;
        private readonly UIA3Automation automation;

        public UiSession(Process process)
        {
            var pid = process.Id;

            app = Application.Attach(process);

            automation = new UIA3Automation();
            root = UiElement.New(app.GetMainWindow(automation));

            //The Process object messes up after we get the main window
            Process = Process.GetProcessById(pid);
        }

        public IUiElement FromHandle(IntPtr hwnd)
        {
            var raw = automation.FromHandle(hwnd);

            return UiElement.New(raw);
        }

        public void Dispose()
        {
            automation?.Dispose();
            app?.Dispose();
            Process?.Dispose();
        }
    }
}
