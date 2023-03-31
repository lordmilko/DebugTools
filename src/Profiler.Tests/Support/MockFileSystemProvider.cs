using DebugTools.PowerShell;

namespace Profiler.Tests
{
    class MockFileSystemProvider : IFileSystemProvider
    {
        private bool fileExists;
        private bool folderExists;

        public MockFileSystemProvider(bool fileExists, bool folderExists)
        {
            this.fileExists = fileExists;
            this.folderExists = folderExists;
        }

        public bool FileExists(string path) => fileExists;

        public bool DirectoryExists(string path) => folderExists;
    }
}
