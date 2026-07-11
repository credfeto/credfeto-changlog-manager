using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using BenchmarkDotNet.Attributes;
using Credfeto.ChangeLog.Models;
using Credfeto.ChangeLog.Services;

namespace Credfeto.ChangeLog.BenchMark.Tests.Bench;

[SimpleJob]
[MemoryDiagnoser(false)]
[SuppressMessage(category: "codecracker.CSharp", checkId: "CC0091:MarkMembersAsStatic", Justification = "Benchmark")]
[SuppressMessage(
    category: "FunFair.CodeAnalysis",
    checkId: "FFS0012: Make sealed static or abstract",
    Justification = "Benchmark"
)]
public class MoveTrailingBlanksBenchmark
{
    private static readonly string ManyTrailingBlanksChangeLog = BuildChangeLog(trailingBlankLines: 200);

    [Benchmark]
    public ChangeLogDocument ParseAsync_ManyTrailingBlanks()
    {
        return ParseSync(ManyTrailingBlanksChangeLog);
    }

    private static string BuildChangeLog(int trailingBlankLines)
    {
        StringBuilder builder = new();
        builder.AppendLine("# Changelog");
        builder.AppendLine();
        builder.AppendLine("## [Unreleased]");
        builder.AppendLine("### Added");
        builder.AppendLine("- Some unreleased item");

        for (int i = 0; i < trailingBlankLines; ++i)
        {
            builder.AppendLine();
        }

        builder.AppendLine("<!--");
        builder.AppendLine("Releases that have at least been deployed to staging.");
        builder.AppendLine("-->");
        builder.AppendLine();
        builder.AppendLine("## [0.0.0] - Project created");

        return builder.ToString();
    }

    [SuppressMessage(
        category: "Meziantou.Analyzer",
        checkId: "MA0045:Use async overload",
        Justification = "Benchmark setup requires synchronous parse of a pure ValueTask.FromResult"
    )]
    [SuppressMessage(
        category: "Microsoft.VisualStudio.Threading.Analyzers",
        checkId: "VSTHRD002",
        Justification = "Benchmark setup requires synchronous parse of a pure ValueTask.FromResult"
    )]
    [SuppressMessage(
        category: "Microsoft.Reliability",
        checkId: "CA2012:UseValueTasksCorrectly",
        Justification = "ChangeLogParser.ParseAsync always returns ValueTask.FromResult — already completed"
    )]
    private static ChangeLogDocument ParseSync(string content) =>
        new ChangeLogParser()
            .ParseAsync(content: content, cancellationToken: CancellationToken.None)
            .GetAwaiter()
            .GetResult();
}
