using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management.Automation;
using DebugTools.Profiler;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsData.Import, "DbgProfilerStackTrace")]
    public class ImportDbgProfilerStackTrace : ProfilerCmdlet
    {
        [Alias("FullName")]
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        public string Path { get; set; }

        protected override void ProcessRecord()
        {
            var reader = new FrameXmlReader();

            using (var fs = GetStream())
            {
                var results = reader.Read(fs);

                var session = new FileProfilerSession(Path);

                var threads = new List<ThreadStack>();

                foreach (var frame in results)
                {
                    ThreadStack stack = frame is IRootFrame r
                        ? new ThreadStack(false, r.ThreadId)
                        : new ThreadStack(false, -1);

                    stack.Current = frame;

                    threads.Add(stack);
                }

                foreach (var kv in reader.Methods)
                    session.Methods[kv.Key] = (IMethodInfoInternal) kv.Value;

                session.LastTrace = threads.ToArray();

                var existing = DebugToolsSessionState.ProfilerSessions.Select((v, i) => new { v.Name, i }).Where(v => v.Name == Path).LastOrDefault();

                if (existing != null)
                {
                    WriteWarning($"Overwriting existing session '{existing.Name}'.");
                    DebugToolsSessionState.ProfilerSessions[existing.i] = session;
                }
                else
                    DebugToolsSessionState.ProfilerSessions.Add(session);

                WriteObject(session);
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
