﻿using System.Diagnostics;
using DebugTools.Ui;

namespace DebugTools
{
    class UiLocalDbgSessionProvider : LocalDbgSessionProvider<LocalUiSession>
    {
        public UiLocalDbgSessionProvider() : base(DbgServiceType.Ui, "Process")
        {
        }

        protected override LocalUiSession CreateServiceInternal(Process process, bool debugHost)
        {
            var pid = process.Id;

            return new LocalUiSession(
                process,
                needDebug =>
                {
                    //When we create the UIAutomation resources, our existing Process
                    //object becomes invalid for some reason
                    var localProcess = Process.GetProcessById(pid);

                    var host = Store.GetDetectedHost(localProcess, needDebug);

                    host.CreateUiMessageSession(localProcess.Id, needDebug);

                    return host;
                }
            );
        }

        protected override bool IsAlive(LocalUiSession service) => !service.Process.HasExited;
    }
}
