using System;
using System.Collections.Generic;
using System.Linq;
using DebugTools.Profiler;
using DebugTools.SOS;

namespace DebugTools.PowerShell
{
    static class DebugToolsSessionState
    {
        internal static List<ProfilerSession> ProfilerSessions = new List<ProfilerSession>();

        internal static List<SOSProcess> SOSProcesses = new List<SOSProcess>();

        internal static ProfilerSession GetImplicitProfilerSession()
        {
            //Check for alive

            var sessions = ProfilerSessions.Where(i => !i.Process.HasExited).ToArray();

            if (sessions.Length == 1)
                return sessions[0];

            if (sessions.Length > 1)
                throw new InvalidOperationException("Cannot execute cmdlet: no -Session was specified and more than one Session belonging to active processes was found in the PowerShell session.");

            //Check for dead

            if (ProfilerSessions.Count == 1)
                return ProfilerSessions[0];

            if (ProfilerSessions.Count > 0)
                return ProfilerSessions.Last(); //All of the sessions have ended, so take the last one

            throw new InvalidOperationException($"Cannot execute cmdlet: no -Session was specified and no global Session could be found in the PowerShell session.");
        }

        internal static SOSProcess GetImplicitSOSProcess()
        {
            //Check for alive

            var processes = SOSProcesses.Where(i => !i.Process.HasExited).ToArray();

            if (processes.Length == 1)
                return processes[0];

            if (processes.Length > 1)
                throw new InvalidOperationException($"Cannot execute cmdlet: no -Process was specified and more than one Process belonging to active processes was found in the PowerShell session.");

            //Check for dead

            if (SOSProcesses.Count > 0)
                throw new InvalidOperationException($"Cannot execute cmdlet: no -Process was specified and all previous SOS Processes have now terminated.");

            throw new InvalidOperationException($"Cannot execute cmdlet: no -Session was specified and no global Session could be found in the PowerShell session.");
        }
    }
}
