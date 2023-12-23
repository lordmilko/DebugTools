using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Management.Automation;
using DebugTools.Profiler;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsData.Export, "DbgProfilerStackTrace")]
    public class ExportDbgProfilerStackTrace : ProfilerSessionCmdlet
    {
        [Parameter(Mandatory = false, ValueFromPipeline = true)]
        public IFrame Frame { get; set; }

        [Parameter(Mandatory = true, Position = 0)]
        public string Path { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Raw { get; set; }

        private List<IFrame> frames = new List<IFrame>();

        protected override void ProcessRecordEx()
        {
            if (Frame != null)
                frames.Add(Frame);
            else
            {
                if (Session.LastTrace == null)
                    WriteWarning("Last trace is empty. Did you forget to record a trace?");
                else
                {
                    foreach (var frame in Session.LastTrace)
                    {
                        frames.Add(frame.Root);
                    }
                }
            }
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
            var directory = System.IO.Path.GetDirectoryName(path);

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var fs = File.OpenWrite(path);

            if (Raw)
                return fs;

            var gz = new GZipStream(fs, CompressionMode.Compress);

            return gz;
        }
    }
}
