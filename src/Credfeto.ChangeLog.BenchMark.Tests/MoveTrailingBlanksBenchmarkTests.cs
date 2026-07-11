using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using Credfeto.ChangeLog.BenchMark.Tests.Bench;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.BenchMark.Tests;

public sealed class MoveTrailingBlanksBenchmarkTests : LoggingTestBase
{
    // Baseline measured after replacing the per-blank Insert(0, ...)/RemoveAt loop with a
    // count-then-copy-then-RemoveRange pass in ChangeLogParser.MoveTrailingBlanks (issue #254).
    // Allocations are unaffected by this change (both approaches only allocate on List<T> backing-array
    // growth) — this baseline guards against allocation regressions, not the CPU-time win, which was
    // measured separately: ParseAsync over a 200-trailing-blank-line fixture averaged ~13.5us/op after
    // the fix vs ~18.3us/op before it (BenchmarkDotNet SimpleJob, allocations identical at ~24.5KB/op
    // in both cases).
    // This limit includes a 25% margin to allow for minor variation across machines.
    private const long MAX_ALLOCATED_BYTES_MANY_TRAILING_BLANKS = 30650;

    public MoveTrailingBlanksBenchmarkTests(ITestOutputHelper output)
        : base(output) { }

    [Fact]
    public void RunBenchmark()
    {
        (Summary summary, AccumulationLogger logger) = Benchmark<MoveTrailingBlanksBenchmark>();

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
            nameof(MoveTrailingBlanksBenchmark.ParseAsync_ManyTrailingBlanks) =>
                MAX_ALLOCATED_BYTES_MANY_TRAILING_BLANKS,
            _ => long.MaxValue,
        };
    }
}
