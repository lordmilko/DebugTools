using System;
using System.IO;

namespace DebugTools.PowerShell
{
    public class ProfilerInfo
    {
        public static readonly string Profilerx86;
        public static readonly string Profilerx64;

        public static readonly Guid Guid = new Guid("9FA9EA80-BE5D-419E-A667-15A672CBD280");

        static ProfilerInfo()
        {
            var dll = new Uri(typeof(ProfilerInfo).Assembly.CodeBase);
            var root = dll.Host + dll.PathAndQuery + dll.Fragment;
            var rootStr = Uri.UnescapeDataString(root);

            var installationRoot = Path.GetDirectoryName(rootStr);

            var profilerName = "Profiler.{0}.dll";

            Profilerx86 = Path.Combine(installationRoot, "x86", string.Format(profilerName, "x86"));
            Profilerx64 = Path.Combine(installationRoot, "x64", string.Format(profilerName, "x64"));
        }
    }
}