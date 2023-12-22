using System;
using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using DebugTools.Profiler;
using Perfolizer.Horology;

namespace Profiler.Benchmarks
{
    internal class ProfilerConfig : ManualConfig
    {
        private static string profilerx86;
        private static string profilerx64;

        private static EnvironmentVariable EnvTraceStart = new EnvironmentVariable("DEBUGTOOLS_TRACESTART", "1");
        private static EnvironmentVariable EnvDetailed = new EnvironmentVariable("DEBUGTOOLS_DETAILED", "1");

        private static EnvironmentVariable EnvModuleBlacklist()
        {
            return new EnvironmentVariable("DEBUGTOOLS_MODULEBLACKLIST", new MatchCollection {{MatchKind.All, string.Empty}}.ToString());
        }

        private static EnvironmentVariable EnvModuleWhitelist()
        {
            return new EnvironmentVariable("DEBUGTOOLS_MODULEWHITELIST", new MatchCollection { { MatchKind.ModuleName, "Profiler.Benchmarks.exe" } }.ToString());
        }

        static ProfilerConfig()
        {
            var dll = new Uri(typeof(ProfilerConfig).Assembly.CodeBase);
            var root = dll.Host + dll.PathAndQuery + dll.Fragment;
            var rootStr = Uri.UnescapeDataString(root);

            var installationRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(rootStr), ".."));

            var profilerName = "Profiler.{0}.dll";

            profilerx86 = Path.Combine(installationRoot, "DebugTools", "x86", string.Format(profilerName, "x86"));
            profilerx64 = Path.Combine(installationRoot, "DebugTools", "x64", string.Format(profilerName, "x64"));
        }

        public ProfilerConfig()
        {
            var defaultJob = Job.ShortRun
                .WithIterationTime(TimeInterval.FromMilliseconds(125))
                .DontEnforcePowerPlan();

            WithOrderer(new OrderFixer(this));

            HideColumns(Column.EnvironmentVariables);

            WithSummaryStyle(BenchmarkDotNet.Reports.SummaryStyle.Default.WithMaxParameterColumnWidth(20));

            AddJob(defaultJob.WithId("Normal"));

            AddJob(defaultJob.WithEnvironmentVariables(BuildEnvVars()).WithId("ELT"));

            var j = GetJobs();

            AddJob(defaultJob.WithEnvironmentVariables(BuildEnvVars(EnvDetailed)).WithId("Detailed_ELT"));

            AddJob(defaultJob.WithEnvironmentVariables(BuildEnvVars(
                EnvTraceStart
            )).WithId("TraceStart"));

            AddJob(defaultJob.WithEnvironmentVariables(BuildEnvVars(
                EnvTraceStart,
                EnvDetailed
            )).WithId("Detailed_TraceStart"));
        }

        private EnvironmentVariable[] BuildEnvVars(params EnvironmentVariable[] additionalVars)
        {
            var list = new List<EnvironmentVariable>
            {
                new EnvironmentVariable("COR_ENABLE_PROFILING", "1"),
                new EnvironmentVariable("COR_PROFILER", "{9FA9EA80-BE5D-419E-A667-15A672CBD280}"),
                new EnvironmentVariable("COR_PROFILER_PATH_32", profilerx86),
                new EnvironmentVariable("COR_PROFILER_PATH_64", profilerx64),
                EnvModuleBlacklist(),
                EnvModuleWhitelist()
            };

            list.AddRange(additionalVars);

            return list.ToArray();
        }
    }
}
