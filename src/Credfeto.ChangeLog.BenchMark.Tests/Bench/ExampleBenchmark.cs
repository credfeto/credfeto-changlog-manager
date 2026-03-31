using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;

namespace Credfeto.ChangeLog.BenchMark.Tests.Bench;

[SimpleJob]
[MemoryDiagnoser(false)]
[SuppressMessage(category: "codecracker.CSharp", checkId: "CC0091:MarkMembersAsStatic", Justification = "Benchmark")]
[SuppressMessage(
    category: "FunFair.CodeAnalysis",
    checkId: "FFS0012: Make sealed static or abstract",
    Justification = "Benchmark"
)]
public class ExampleBenchmark
{
    [Benchmark]
    public string Test()
    {
        return "Test Performance Here";
    }
}