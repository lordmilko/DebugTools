using System;
using System.Diagnostics;
using DebugTools.PowerShell;
using DebugTools.Profiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Profiler.Tests
{
    [TestClass]
    public class ExportPathResolverTests
    {
        [TestMethod]
        public void ExportPathResolver_DriveRoot()
        {
            Test("Z:", $"Z:\\StackTrace_{Process.GetCurrentProcess().Id}_{Process.GetCurrentProcess().ProcessName}_{DateTime.Now:yyyy-MM-dd_HHmm}h.xml");
        }

        [TestMethod]
        public void ExportPathResolver_DriveRootSlash()
        {
            Test("Z:\\", $"Z:\\StackTrace_{Process.GetCurrentProcess().Id}_{Process.GetCurrentProcess().ProcessName}_{DateTime.Now:yyyy-MM-dd_HHmm}h.xml");
        }

        [TestMethod]
        public void ExportPathResolver_FolderNoSlash()
        {
            Test("Z:\\foo", $"Z:\\foo\\StackTrace_{Process.GetCurrentProcess().Id}_{Process.GetCurrentProcess().ProcessName}_{DateTime.Now:yyyy-MM-dd_HHmm}h.xml", folderExists: true);
        }

        [TestMethod]
        public void ExportPathResolver_FolderTrailingSlash_Exists()
        {
            Test("Z:\\foo\\", $"Z:\\foo\\StackTrace_{Process.GetCurrentProcess().Id}_{Process.GetCurrentProcess().ProcessName}_{DateTime.Now:yyyy-MM-dd_HHmm}h.xml", folderExists: true);
        }

        [TestMethod]
        public void ExportPathResolver_FolderTrailingSlash_DoesntExist()
        {
            Test("Z:\\foo\\", $"Z:\\foo\\StackTrace_{Process.GetCurrentProcess().Id}_{Process.GetCurrentProcess().ProcessName}_{DateTime.Now:yyyy-MM-dd_HHmm}h.xml");
        }

        [TestMethod]
        public void ExportPathResolver_ExistingFile_CorrectExtension()
        {
            Test("Z:\\foo.xml", $"Z:\\foo.xml", fileExists: true);
        }

        [TestMethod]
        public void ExportPathResolver_ExistingFile_WrongExtension()
        {
            Test("Z:\\foo.xml", $"Z:\\foo.xml.gz", ExportMode.GZip, fileExists: true);
        }

        [TestMethod]
        public void ExportPathResolver_NewFile_CorrectExtension()
        {
            Test("Z:\\foo.xml", $"Z:\\foo.xml");
        }

        [TestMethod]
        public void ExportPathResolver_NewFile_WrongExtension()
        {
            Test("Z:\\foo.xml", $"Z:\\foo.xml.gz", ExportMode.GZip);
        }

        [TestMethod]
        public void ExportPathResolver_NewFile_NoExtension()
        {
            Test("Z:\\foo", $"Z:\\foo.xml");
        }

        [TestMethod]
        public void ExportPathResolver_NullProfilerSession()
        {
            Test("Z:\\foo\\", $"Z:\\foo\\StackTrace_{DateTime.Now:yyyy-MM-dd_HHmm}h.xml", wantSession: false);
        }

        private void Test(string input, string expected, ExportMode mode = ExportMode.Xml, bool wantSession = true, bool fileExists = false, bool folderExists = false)
        {
            var session = wantSession ? new MockProfilerSession(new RootFrame[0]) : null;

            var resolver = new ExportPathResolver(input, mode, session, new MockFileSystemProvider(fileExists, folderExists));

            var actual = resolver.Resolve();

            Assert.AreEqual(expected, actual);
        }
    }
}
