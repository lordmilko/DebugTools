using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

namespace Profiler.Benchmarks
{
    [Config(typeof(ProfilerConfig))]
    public class ProfilerBenchmarks
    {
        [Params("foo")]
        public string Input { get; set; }

        [Benchmark]
        public void DoNothing()
        {
        }

        [Benchmark]
        public int ReturnInt()
        {
            return 1;
        }

        [Benchmark]
        public void TakeString()
        {
            TakeStringInternal(Input);
        }

        [Benchmark]
        public void TakeAndReturnString()
        {
            TakeAndReturnString(Input);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void TakeStringInternal(string str)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private string TakeAndReturnString(string str)
        {
            return str;
        }
    }
}
