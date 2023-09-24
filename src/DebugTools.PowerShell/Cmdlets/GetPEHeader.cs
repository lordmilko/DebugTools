using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using ChaosLib.Memory;
using DebugTools.Memory;
using ChaosPEFile = ChaosLib.Metadata.PEFile;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "PEHeader")]
    public class GetPEHeader : DebugToolsCmdlet
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public Process Process { get; set; }

        [Parameter(Mandatory = false, Position = 0)]
        public string ModuleName { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Exports { get; set; }

        protected override void ProcessRecord()
        {
            var dataTargetStream = new DataTargetMemoryStream(new DataTarget(Process));

            var modules = new List<ProcessModule>();

            if (ModuleName != null)
            {
                var wildcard = new WildcardPattern(ModuleName, WildcardOptions.IgnoreCase);

                var matches = Process.GetProcessById(Process.Id).Modules.Cast<ProcessModule>().Where(m => wildcard.IsMatch(m.ModuleName)).ToArray();

                foreach (var match in matches)
                    modules.Add(match);
            }
            else
            {
                modules.Add(Process.MainModule);
            }

            foreach (var module in modules)
            {
                var relativeStream = new RelativeToAbsoluteStream(dataTargetStream, (long)module.BaseAddress);
                relativeStream.Seek(0, SeekOrigin.Begin);

                var peFile = new ChaosPEFile(relativeStream, true);

                if (Exports)
                {
                    var exports = peFile.ExportDirectory?.Exports;

                    if (exports != null)
                    {
                        foreach (var export in exports)
                            WriteObject(export);
                    }
                }
                else
                {
                    new PSObject(peFile).Properties.Add(new PSNoteProperty("ModuleName", module.ModuleName));

                    WriteObject(peFile);
                }
            }
        }
    }
}
