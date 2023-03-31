using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace DebugTools
{
    public class DbgEngProvider
    {
        public static string GetWinDbg(bool is32Bit)
        {
            var folder = Path.GetDirectoryName(GetDbgEngPath(is32Bit));

            return Path.Combine(folder, "windbg.exe");
        }

        private static string GetDbgEngPath() => GetDbgEngPath(IntPtr.Size == 4);

        private static string GetDbgEngPath(bool is32Bit)
        {
            var programFiles = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                Environment.GetFolderPath(Environment.SpecialFolder.Programs)
            };

            var candidates = new List<string>();

            foreach (var baseFolder in programFiles)
            {
                if (Directory.Exists(baseFolder))
                {
                    var windowsKits = Path.Combine(baseFolder, "Windows Kits");

                    if (Directory.Exists(windowsKits))
                    {
                        var windowsKitsSubfolders = Directory.GetDirectories(windowsKits);

                        foreach (var windowsKitsSubfolder in windowsKitsSubfolders)
                        {
                            GetDbgEngDebuggersPath(windowsKitsSubfolder, candidates, is32Bit);
                        }
                    }
                }
            }

            GetDbgEngDebuggersPath("C:\\", candidates, is32Bit);

            if (candidates.Count == 0)
                throw new NotImplementedException();

            if (candidates.Count == 1)
                return candidates[0];

            var versions = candidates.Select(c => FileVersionInfo.GetVersionInfo(c)).OrderByDescending(v => new Version(v.ProductVersion)).ToArray();

            var first = versions.First();

            return first.FileName;
        }

        private static void GetDbgEngDebuggersPath(string parent, List<string> candidates, bool is32Bit)
        {
            var debuggersFolder = Path.Combine(parent, "Debuggers");

            if (Directory.Exists(debuggersFolder))
            {
                var archFolder = Path.Combine(debuggersFolder, is32Bit ? "x86" : "x64");

                if (Directory.Exists(archFolder))
                {
                    var dbgEng = Path.Combine(archFolder, "dbgeng.dll");

                    if (File.Exists(dbgEng))
                        candidates.Add(dbgEng);
                }
            }
        }
    }
}
