using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using Credfeto.ChangeLog.BenchMark.Tests.Bench;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.BenchMark.Tests;

public sealed class EnsureUnreleasedSectionsBenchmarkTests : LoggingTestBase
{
    // Baselines measured after replacing per-call HashSet with static FrozenSet (issue #253).
    // These limits include a 25% margin to allow for minor variation across machines.
    private const long MAX_ALLOCATED_BYTES_ALL_SECTIONS_CORRECT = 8000;
    private const long MAX_ALLOCATED_BYTES_OUT_OF_ORDER_AND_MISSING = 8550;

    public EnsureUnreleasedSectionsBenchmarkTests(ITestOutputHelper output)
        : base(output)
    {
    }

    [Fact]
    public void RunBenchmark()
    {
        (Summary summary, AccumulationLogger logger) = Benchmark<EnsureUnreleasedSectionsBenchmark>();

        this.Output.WriteLine(logger.GetLog());

        foreach (BenchmarkReport report in summary.Reports)
        {
            long? allocatedBytes = report.GcStats.GetBytesAllocatedPerOperation(report.BenchmarkCase);
            string methodName = report.BenchmarkCase.Descriptor.WorkloadMethod.Name;
            string allocationText = allocatedBytes.HasValue
                ? allocatedBytes.Value.ToString(provider: System.Globalization.CultureInfo.InvariantCulture) + " bytes/op"
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
            nameof(EnsureUnreleasedSectionsBenchmark.EnsureUnreleasedSections_AllSectionsCorrect) => MAX_ALLOCATED_BYTES_ALL_SECTIONS_CORRECT,
            nameof(EnsureUnreleasedSectionsBenchmark.EnsureUnreleasedSections_OutOfOrderAndMissing) => MAX_ALLOCATED_BYTES_OUT_OF_ORDER_AND_MISSING,
            _ => long.MaxValue,
        };
    }
}
