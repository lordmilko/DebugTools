using System;
using System.IO;
using System.IO.Compression;
using System.Management.Automation;
using DebugTools.Profiler;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsData.Import, "DbgProfilerStackTrace")]
    public class ImportDbgProfilerStackTrace : ProfilerCmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        public string Path { get; set; }

        protected override void ProcessRecord()
        {
            var reader = new FrameXmlReader();

            using (var fs = GetStream())
            {
                var results = reader.Read(fs);

                foreach (var result in results)
                    WriteObject(result);
            }
        }

        private Stream GetStream()
        {
            if (!File.Exists(Path))
                throw new FileNotFoundException("Could not find the specified file", Path);

            var ext = System.IO.Path.GetExtension(Path);

            switch (ext)
            {
                case ".gz":
                    return new GZipStream(File.OpenRead(Path), CompressionMode.Decompress);

                case ".xml":
                    return File.OpenRead(Path);

                default:
                    throw new InvalidOperationException($"Don't know how to handle file with extension '{ext}'.");
            }
        }
    }
}
