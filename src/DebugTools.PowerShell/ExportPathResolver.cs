using System;
using System.ComponentModel;
using System.IO;
using DebugTools.Profiler;

namespace DebugTools.PowerShell
{
    enum ExportMode
    {
        [Description(".gz")]
        GZip,

        [Description(".xml")]
        Xml
    }

    interface IFileSystemProvider
    {
        bool FileExists(string path);
        bool DirectoryExists(string path);
    }

    class FileSystemProvider : IFileSystemProvider
    {
        public bool FileExists(string path) => File.Exists(path);

        public bool DirectoryExists(string path) => Directory.Exists(path);
    }

    class ExportPathResolver
    {
        private string path;
        private string desiredExtension;
        private ProfilerSession session;
        private IFileSystemProvider provider;

        public ExportPathResolver(string path, ExportMode mode, ProfilerSession session, IFileSystemProvider provider)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException(nameof(path));

            this.path = path;
            this.session = session;
            desiredExtension = mode.GetDescription();
            this.provider = provider;
        }

        public string Resolve()
        {
            //Is it an existing file?
            if (provider.FileExists(path))
            {
                var ext = Path.GetExtension(path).ToLower();

                if (ext != desiredExtension)
                    path += desiredExtension;

                return path;
            }

            if (path.EndsWith(":"))
                path += Path.DirectorySeparatorChar;

            if (provider.DirectoryExists(path))
                return Path.Combine(path, GetRandomFileName());

            var fileName = Path.GetFileName(path);

            if (string.IsNullOrEmpty(fileName))
            {
                //This isn't a directory that already exists, and there is no filename, so it must be a drive root

                return Path.Combine(path, GetRandomFileName());
            }

            var actualExtension = Path.GetExtension(path).ToLower();

            if (actualExtension != desiredExtension)
                return path + desiredExtension;

            return path;
        }

        private string GetRandomFileName()
        {
            if (session == null)
                return $"StackTrace_{DateTime.Now:yyyy-MM-dd_HHmm}h{desiredExtension}";

            return $"StackTrace_{session.Process.Id}_{session.Process.ProcessName}_{DateTime.Now:yyyy-MM-dd_HHmm}h{desiredExtension}";
        }
    }
}
