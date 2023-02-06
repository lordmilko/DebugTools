using System;
using System.IO;
using ClrDebug;
using Microsoft.Diagnostics.Runtime.Utilities;

namespace DebugTools
{
    [Serializable]
    public class SymbolModule
    {
        public string Name { get; }

        public string FullName { get; }

        public ulong Start { get; }

        public ulong End { get; }

        public int Length { get; }

        public bool SymbolsLoaded { get; set; }

        public CLRDATA_ADDRESS OriginalBase { get; }

        public unsafe SymbolModule(RTL_PROCESS_MODULE_INFORMATION* pModuleInfo)
        {
            Name = pModuleInfo->Name;
            FullName = pModuleInfo->FullName;
            Start = (ulong) pModuleInfo->ImageBase.ToInt64();
            End = Start + pModuleInfo->ImageSize;
            Length = (int) pModuleInfo->ImageSize;

            using (var fileStream = File.Open(FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var peFile = new PEImage(fileStream);

                OriginalBase = peFile.OptionalHeader.ImageBase;
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
