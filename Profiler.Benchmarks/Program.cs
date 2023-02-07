using BenchmarkDotNet.Running;

namespace Profiler.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<ProfilerBenchmarks>(args: args);
        }
    }
}
