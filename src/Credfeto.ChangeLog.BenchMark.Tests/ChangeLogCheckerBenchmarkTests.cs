using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using Credfeto.ChangeLog.BenchMark.Tests.Bench;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.BenchMark.Tests;

public sealed class ChangeLogCheckerBenchmarkTests : LoggingTestBase
{
    // Baseline measured after adding a `paths` filter to the diff comparison in ChangeLogChecker (issue #331),
    // which limits the diff to the changelog file instead of scanning the whole working tree.
    // These limits include a 25% margin to allow for minor variation across machines.
    private const long MAX_ALLOCATED_BYTES_CHANGE_LOG_UNCHANGED = 44062;
    private const long MAX_ALLOCATED_BYTES_UNRELEASED_SECTION_CHANGED = 54560;

    public ChangeLogCheckerBenchmarkTests(ITestOutputHelper output)
        : base(output) { }

    [Fact]
    public void RunBenchmark()
    {
        (Summary summary, AccumulationLogger logger) = Benchmark<ChangeLogCheckerBenchmark>();

        this.Output.WriteLine(logger.GetLog());

        foreach (BenchmarkReport report in summary.Reports)
        {
            long? allocatedBytes = report.GcStats.GetBytesAllocatedPerOperation(report.BenchmarkCase);
            string methodName = report.BenchmarkCase.Descriptor.WorkloadMethod.Name;
            string allocationText = allocatedBytes.HasValue
                ? allocatedBytes.Value.ToString(provider: System.Globalization.CultureInfo.InvariantCulture)
                    + " bytes/op"
                : "N/A";
            this.Output.WriteLine(methodName + ": " + allocationText);

            if (allocatedBytes.HasValue)
            {
                long maxAllowed = GetMaxAllocatedBytes(methodName);
                Assert.True(
                    condition: allocatedBytes.Value <= maxAllowed,
                    userMessage: $"{methodName} allocated {allocatedBytes.Value} bytes/op, which exceeds the baseline limit of {maxAllowed} bytes/op"
                );
            }
        }
    }

    private static long GetMaxAllocatedBytes(string methodName)
    {
        return methodName switch
        {
            nameof(ChangeLogCheckerBenchmark.ChangeLogModifiedInReleaseSection_ChangeLogUnchangedAsync) =>
                MAX_ALLOCATED_BYTES_CHANGE_LOG_UNCHANGED,
            nameof(ChangeLogCheckerBenchmark.ChangeLogModifiedInReleaseSection_UnreleasedSectionChangedAsync) =>
                MAX_ALLOCATED_BYTES_UNRELEASED_SECTION_CHANGED,
            _ => long.MaxValue,
        };
    }
}
