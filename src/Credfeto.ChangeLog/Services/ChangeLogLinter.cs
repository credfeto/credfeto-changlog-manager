using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Extensions;
using Credfeto.ChangeLog.Models;

namespace Credfeto.ChangeLog.Services;

[SuppressMessage(category: "Microsoft.Performance", checkId: "CA1812: Avoid uninstantiated internal classes", Justification = "Registered in DI")]
internal sealed partial class ChangeLogLinter : IChangeLogLinter
{
    private static readonly string[] RequiredSections =
    [
        "Security",
        "Added",
        "Fixed",
        "Changed",
        "Removed"
    ];

    private static readonly string[] KnownOptionalSections = ["Deprecated", "Deployment Changes"];

    private readonly IChangeLogStorage _loader;

    public ChangeLogLinter(IChangeLogStorage loader)
    {
        this._loader = loader;
    }

    public async ValueTask<IReadOnlyList<LintError>> LintFileAsync(string changeLogFileName, IReadOnlyCollection<string>? additionalSections, CancellationToken cancellationToken)
    {
        string content = await this._loader.LoadTextAsync(changeLogFileName, cancellationToken);

        return Lint(content: content, additionalSections: additionalSections);
    }

    internal static IReadOnlyList<LintError> Lint(string content, IReadOnlyCollection<string>? additionalSections = null)
    {
        List<LintError> errors = [];

        string[] lines = NormalizeLines(content);

        CheckUnreleasedSectionPresent(lines: lines, errors: errors, additionalSections: additionalSections);

        CheckBlankLineAfterHeadings(lines: lines, errors: errors);

        CheckVersionHeaders(lines: lines, errors: errors);

        return errors;
    }

    private static string[] NormalizeLines(string content)
    {
        return [.. content.SplitToLines()];
    }
}
