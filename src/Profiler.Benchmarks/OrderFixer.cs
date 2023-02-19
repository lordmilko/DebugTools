using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace Profiler.Benchmarks
{
    class OrderFixer : DefaultOrderer
    {
        private ManualConfig config;

        public OrderFixer(ManualConfig config)
        {
            this.config = config;
        }

        public override IEnumerable<BenchmarkCase> GetSummaryOrder(ImmutableArray<BenchmarkCase> benchmarksCases, Summary summary)
        {
            var original = config.GetJobs().ToArray();

            var results = new List<BenchmarkCase>();

            var groups = benchmarksCases.GroupBy(c => c.Descriptor).ToArray();

            //Despite the fact we specify we want the declared order, for some reason sometimes it's in an alphabetical initial order. Debugging with
            //dnSpy shows it in the correct order...very strange, so we'll fix it up manually if need be
            foreach (var descriptor in groups)
            {
                foreach (var item in original)
                {
                    var match = descriptor.Single(c => c.Job == item);

                    results.Add(match);
                }
            }

            return results;
        }
    }
}
