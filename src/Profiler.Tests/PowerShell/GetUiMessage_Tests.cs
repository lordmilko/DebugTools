using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation.Runspaces;
using System.Text.RegularExpressions;
using System.Threading;
using DebugTools;
using DebugTools.PowerShell;
using DebugTools.Ui;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Profiler.Tests
{
    //For some reason PowerShell's formatting engine is getting confused when running multiple tests at once, and objects aren't emitted with the right format,
    //indicating there's some static resource being accessed that pertains to the formatting engine. As such, we don't run these tests in parallel

    [TestClass]
    [DoNotParallelize]
    public class GetUiMessage_Tests : BasePowerShellTest
    {
        [TestMethod]
        public void PowerShell_UiMessage_NoFilters()
        {
            var candidate1 = @"
Window                         Message                       wParam                        lParam                      
------                         -------                       ------                        ------                      
[Window] Document - WordPad    WM_GETICON                    0x2                           0x0";

            var candidate2 = @"
Window                         Message                       wParam                        lParam                      
------                         -------                       ------                        ------                      
[Document] Rich Text Window    WM_GETTEXTLENGTH              0x0                           0x0
";

            Test(
                null,
                new[]{candidate1, candidate2}
            );
        }

        [TestMethod]
        public void PowerShell_UiMessage_SingleType()
        {
            Test(
                new { Type = WM.WM_NULL },
                @"
Window                         Message                       wParam                        lParam                      
------                         -------                       ------                        ------                      
[Window] Document - WordPad    WM_NULL                       0x1                           0x2",
                hwnd => NativeMethods.SendMessageW(hwnd, WM.WM_NULL, new IntPtr(1), new IntPtr(2))
            );
        }

        [TestMethod]
        public unsafe void PowerShell_UiMessage_SingleType_CustomFormat()
        {
            Test(
                new { Type = WM.WM_SETTEXT },
                @"
Window                         Message                       wParam                        Text                        
------                         -------                       ------                        ----                        
[Window] foo                   WM_SETTEXT                    0x0                           foo",
                hwnd =>
                {
                    fixed (char* c = "foo")
                    {
                        NativeMethods.SendMessageW(hwnd, WM.WM_SETTEXT, IntPtr.Zero, (IntPtr) c);
                    }
                }
            );
        }

        [TestMethod]
        public unsafe void PowerShell_UiMessage_TwoTypes_DifferentFormats()
        {
            Test(
                new { Type = new[]{WM.WM_NULL, WM.WM_SETTEXT} },
                @"
Window                         Message                       wParam                        lParam                      
------                         -------                       ------                        ------                      
[Window] foo                   WM_NULL                       0x1                           0x2                         
[Window] foo                   WM_SETTEXT                    0x0                           <address>",
                hwnd =>
                {
                    fixed (char* c = "foo")
                    {
                        NativeMethods.SendMessageW(hwnd, WM.WM_NULL, new IntPtr(1), new IntPtr(2));
                        NativeMethods.SendMessageW(hwnd, WM.WM_SETTEXT, IntPtr.Zero, (IntPtr)c);
                    }
                },
                2
            );
        }

        [TestMethod]
        public unsafe void PowerShell_UiMessage_TwoTypes_SameFormat()
        {
            Test(
                new { Type = new[] { WM.WM_SETCURSOR, WM.WM_MOUSEACTIVATE } },
                @"
Window                         Message                wParam                HitTest               MouseMessage         
------                         -------                ------                -------               ------------         
[Window] Document - WordPad    WM_SETCURSOR           0x0                   HTNOWHERE             WM_NULL              
[Window] Document - WordPad    WM_MOUSEACTIVATE       0x0                   HTNOWHERE             WM_NULL",
                hwnd =>
                {
                    NativeMethods.SendMessageW(hwnd, WM.WM_SETCURSOR, IntPtr.Zero, IntPtr.Zero);
                    NativeMethods.SendMessageW(hwnd, WM.WM_MOUSEACTIVATE, IntPtr.Zero, IntPtr.Zero);
                },
                2
            );
        }

        private void Test(object param, string expected, Action<IntPtr> messageGenerator = null, int recordCount = 1) =>
            Test(param, new[] {expected}, messageGenerator, recordCount);

        private void Test(object param, string[] expected, Action<IntPtr> messageGenerator = null, int recordCount = 1)
        {
            var psi = new ProcessStartInfo("wordpad")
            {
                WindowStyle = ProcessWindowStyle.Minimized
            };

            Process process = null;
            int? processId = null;
            LocalUiSession session = null;

            var cts = new CancellationTokenSource();

            try
            {
                process = Process.Start(psi);
                processId = process.Id;

                session = DebugToolsSessionState.Services.Create<LocalUiSession>(process, false);

                var host = new FakePSHost();

                var invoker = new PowerShellInvoker(
                    initialSessionState => RunspaceFactory.CreateRunspace(
                        host,
                        initialSessionState
                    )
                );

                var ui = (FakePSHostUserInterface) host.UI;

                var dict = new Dictionary<string, object>
                {
                    {"Session", session}
                };

                if (param != null)
                {
                    var properties = param.GetType().GetProperties();

                    foreach (var prop in properties)
                    {
                        var value = prop.GetValue(param);
                        dict[prop.Name] = value;
                    }
                }

                if (messageGenerator != null)
                {
                    var thread = new Thread(() =>
                    {
                        while (!cts.IsCancellationRequested)
                        {
                            messageGenerator(Process.GetProcessById(processId.Value).MainWindowHandle);

                            cts.Token.WaitHandle.WaitOne(100);
                        }
                    });
                    thread.Start();
                }

                invoker.Invoke<object>("Get-UiMessage", dict, outputCmdlets: new[]
                {
                    ("Select-Object", (object) new { First = recordCount }),
                    ("Out-Default", null)
                });

                string Normalize(string value)
                {
                    value = value.Trim();

                    var addresses = Regex.Match(value, "0x2.+$");

                    if (addresses.Success && addresses.Value.Trim().Length > 4)
                    {
                        value = value.Replace(addresses.Value, "<address>");
                    }

                    return value;
                }

                var str = Normalize(ui.Buffer.ToString());

                if (expected.Length == 1)
                    Assert.AreEqual(Normalize(expected[0]), str);
                else
                    Assert.IsTrue(expected.Any(e => Normalize(e) == str));
            }
            finally
            {
                WindowMessageFormatProvider.Instance.Reset();

                cts.Cancel();

                if (session != null)
                    DebugToolsSessionState.Services.Close(processId.Value, session);

                try
                {
                    if (processId != null)
                    {
                        //The existing Process object no longer works
                        Process.GetProcessById(processId.Value).Kill();
                    }
                }
                catch
                {
                }
            }
        }
    }
}
