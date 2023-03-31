using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Management.Automation;
using DebugTools.Profiler;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsData.Export, "DbgProfilerStackTrace")]
    public class ExportDbgProfilerStackTrace : StackFrameCmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        public string Path { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Raw { get; set; }

        private List<IFrame> frames = new List<IFrame>();

        protected override void DoProcessRecordEx()
        {
            frames.Add(Frame);
        }

        protected override void EndProcessing()
        {
            var writer = new FrameXmlWriter(frames.ToArray());

            var path = GetPath();

            using (var stream = GetStream(path))
            {
                writer.Write(stream);
            }

            var file = new FileInfo(path);

            WriteObject(file);
        }

        private string GetPath()
        {
            var resolver = new ExportPathResolver(Path, Raw ? ExportMode.Xml : ExportMode.GZip, Session, new FileSystemProvider());

            return resolver.Resolve();
        }

        private Stream GetStream(string path)
        {
            var fs = File.OpenWrite(path);

            if (Raw)
                return fs;

            var gz = new GZipStream(fs, CompressionMode.Compress);

            return gz;
        }
    }
}
