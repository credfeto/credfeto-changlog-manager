using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CommandLine;

namespace Credfeto.ChangeLog.Cmd;

[SuppressMessage(
    category: "ReSharper",
    checkId: "ClassNeverInstantiated.Global",
    Justification = "Created using reflection"
)]
[SuppressMessage(
    category: "ReSharper",
    checkId: "UnusedAutoPropertyAccessor.Global",
    Justification = "Created using reflection"
)]
public sealed class Options
{
    [Option(shortName: 'f', longName: "changelog", Required = false, HelpText = "The changelog filename to use")]
    public string? ChangeLog { get; init; }

    [Option(
        shortName: 'v',
        longName: "version",
        Group = "Commands",
        Required = false,
        HelpText = "The version to extract"
    )]
    public string? Version { get; init; }

    [Option(
        shortName: 'x',
        longName: "extract",
        Group = "Commands",
        Required = false,
        HelpText = "The filename to write the extracted"
    )]
    public string? Extract { get; init; }

    [Option(
        shortName: 'r',
        longName: "remove",
        Group = "Commands",
        Required = false,
        HelpText = "The entry type to remove"
    )]
    public string? Remove { get; init; }

    [Option(shortName: 'a', longName: "add", Group = "Commands", Required = false, HelpText = "The entry type to add")]
    public string? Add { get; init; }

    [Option(shortName: 'm', longName: "message", Required = false, HelpText = "The message to add")]
    public string? Message { get; init; }

    [Option(
        shortName: 't',
        longName: "check-insert",
        Group = "Commands",
        Required = false,
        HelpText = "The branch to check the changelog again"
    )]
    public string? CheckInsert { get; init; }

    [Option(
        shortName: 'c',
        longName: "create-release",
        Group = "Commands",
        Required = false,
        HelpText = "The release version to create"
    )]
    public string? CreateRelease { get; init; }

    [Option(shortName: 'p', longName: "Pending", Required = false, HelpText = "If the release is pending")]
    public bool Pending { get; init; }

    [Option(
        shortName: 'u',
        longName: "un-released",
        Group = "Commands",
        Required = false,
        HelpText = "Prints the unreleased section to the console."
    )]
    public bool DisplayUnreleased { get; init; }

    [Option(
        shortName: 'l',
        longName: "lint",
        Group = "Commands",
        Required = false,
        HelpText = "Lint the changelog for correctness"
    )]
    public bool Lint { get; init; }

    [Option(longName: "additional-sections", Required = false, Separator = ',', HelpText = "Additional change type sections allowed in unreleased")]
    public IEnumerable<string> AdditionalSections { get; init; } = [];

    [Option(longName: "fix", Required = false, HelpText = "When used with --lint, rewrite the file to fix formatting issues")]
    public bool Fix { get; init; }
}
