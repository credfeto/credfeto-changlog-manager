using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using Credfeto.ChangeLog.BenchMark.Tests.Bench;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.BenchMark.Tests;

public sealed class ExampleBenchmarkTests: LoggingTestBase
{
    public ExampleBenchmarkTests(ITestOutputHelper output)
        : base(output)
    {
    }

    [Fact]
    public void RunBenchmark()
    {
        (Summary _, AccumulationLogger logger) = Benchmark<ExampleBenchmark>();

        this.Output.WriteLine(logger.GetLog());

        // note should define a baseline for how execution and memory usage and asser if memory usage goes up
    }
}