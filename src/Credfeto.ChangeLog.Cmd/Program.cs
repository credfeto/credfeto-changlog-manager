using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Credfeto.ChangeLog.Cmd.Exceptions;
using Credfeto.ChangeLog.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Credfeto.ChangeLog.Cmd;

public static class Program
{
    private const int SUCCESS = 0;
    private const int ERROR = 1;

    private static string FindChangeLog(Options options, IChangeLogDetector detector)
    {
        string? changeLog = options.ChangeLog;

        if (changeLog is not null)
        {
            return changeLog;
        }

        if (detector.TryFindChangeLog(out changeLog))
        {
            return changeLog;
        }

        return ThrowMissingChangelogException();
    }

    [DoesNotReturn]
    private static string ThrowMissingChangelogException()
    {
        throw new MissingChangelogException("Could not find changelog");
    }

    private static async Task ParsedOkAsync(Options options, IServiceProvider services)
    {
        CancellationToken cancellationToken = CancellationToken.None;
        IChangeLogDetector detector = services.GetRequiredService<IChangeLogDetector>();
        ChangeLogLanguage language = services
            .GetRequiredService<IChangeLogLanguageFactory>()
            .Get(ChangeLogLanguageFactory.English);

        if (options.Extract is not null || options.Version is not null)
        {
            EnsureExtractOptionsComplete(options);

            await ExtractChangeLogTextForVersionAsync(
                options: options,
                detector: detector,
                reader: services.GetRequiredService<IChangeLogReader>(),
                cancellationToken: cancellationToken
            );

            return;
        }

        if (options.Add is not null)
        {
            EnsureMessageProvided(options: options, commandOptionName: "--add");

            await AddEntryToUnreleasedChangelogAsync(
                options: options,
                detector: detector,
                language: language,
                updater: services.GetRequiredService<IChangeLogUpdater>(),
                cancellationToken: cancellationToken
            );

            return;
        }

        if (options.Remove is not null)
        {
            EnsureMessageProvided(options: options, commandOptionName: "--remove");

            await RemoveEntryFromUnreleasedChangelogAsync(
                options: options,
                detector: detector,
                language: language,
                updater: services.GetRequiredService<IChangeLogUpdater>(),
                cancellationToken: cancellationToken
            );

            return;
        }

        await ParsedOkContinuationAsync(
            options: options,
            detector: detector,
            language: language,
            services: services,
            cancellationToken: cancellationToken
        );
    }

    private static void EnsureExtractOptionsComplete(Options options)
    {
        if (options.Extract is null)
        {
            throw new InvalidOptionsException("--version requires --extract");
        }

        if (options.Version is null)
        {
            throw new InvalidOptionsException("--extract requires --version");
        }
    }

    private static void EnsureMessageProvided(Options options, string commandOptionName)
    {
        if (options.Message is null)
        {
            throw new InvalidOptionsException($"{commandOptionName} requires --message");
        }
    }

    private static async Task ParsedOkContinuationAsync(
        Options options,
        IChangeLogDetector detector,
        ChangeLogLanguage language,
        IServiceProvider services,
        CancellationToken cancellationToken
    )
    {
        if (options.CheckInsert is not null)
        {
            await CheckInsertPositionAsync(
                options: options,
                detector: detector,
                checker: services.GetRequiredService<IChangeLogChecker>(),
                cancellationToken: cancellationToken
            );

            return;
        }

        if (options.CreateRelease is not null)
        {
            await CreateNewReleaseAsync(
                options: options,
                detector: detector,
                language: language,
                updater: services.GetRequiredService<IChangeLogUpdater>(),
                cancellationToken: cancellationToken
            );

            return;
        }

        if (options.DisplayUnreleased)
        {
            await OutputUnreleasedContentAsync(
                options: options,
                detector: detector,
                reader: services.GetRequiredService<IChangeLogReader>(),
                cancellationToken: cancellationToken
            );

            return;
        }

        if (options.Lint)
        {
            await LintChangeLogAsync(
                options: options,
                detector: detector,
                language: language,
                linter: services.GetRequiredService<IChangeLogLinter>(),
                fixer: services.GetRequiredService<IChangeLogFixer>(),
                cancellationToken: cancellationToken
            );

            return;
        }

        throw new InvalidOptionsException("No recognised command was specified");
    }

    private static ChangeLogLanguage WithAdditionalSections(
        ChangeLogLanguage language,
        IReadOnlyList<string> additionalSections
    )
    {
        if (additionalSections.Count == 0)
        {
            return language;
        }

        return language with
        {
            SectionOrder = [.. language.SectionOrder, .. additionalSections],
        };
    }

    private static async Task LintChangeLogAsync(
        Options options,
        IChangeLogDetector detector,
        ChangeLogLanguage language,
        IChangeLogLinter linter,
        IChangeLogFixer fixer,
        CancellationToken cancellationToken
    )
    {
        string changeLog = FindChangeLog(options, detector);
        Console.WriteLine($"Using Changelog {changeLog}");

        ChangeLogLanguage effectiveLanguage = WithAdditionalSections(
            language: language,
            additionalSections: [.. options.AdditionalSections]
        );

        IReadOnlyList<LintError> errors = await linter.LintAsync(
            changeLogFileName: changeLog,
            language: effectiveLanguage,
            cancellationToken: cancellationToken
        );

        if (errors.Count == 0)
        {
            Console.WriteLine("Changelog is valid");

            return;
        }

        foreach (LintError error in errors)
        {
            Console.WriteLine($"Line {error.LineNumber}: {error.Message}");
        }

        if (options.Fix)
        {
            await FixAndRelintAsync(
                changeLog: changeLog,
                language: effectiveLanguage,
                linter: linter,
                fixer: fixer,
                cancellationToken: cancellationToken
            );

            return;
        }

        throw new ChangeLogInvalidFailedException("Changelog has lint errors");
    }

    private static async Task FixAndRelintAsync(
        string changeLog,
        ChangeLogLanguage language,
        IChangeLogLinter linter,
        IChangeLogFixer fixer,
        CancellationToken cancellationToken
    )
    {
        Console.WriteLine("Applying fixes...");

        await fixer.FixAsync(changeLogFileName: changeLog, language: language, cancellationToken: cancellationToken);

        Console.WriteLine("Fixed. Re-linting...");

        IReadOnlyList<LintError> remainingErrors = await linter.LintAsync(
            changeLogFileName: changeLog,
            language: language,
            cancellationToken: cancellationToken
        );

        if (remainingErrors.Count == 0)
        {
            Console.WriteLine("Changelog is valid after fix");

            return;
        }

        Console.WriteLine("Remaining errors after fix:");

        foreach (LintError error in remainingErrors)
        {
            Console.WriteLine($"Line {error.LineNumber}: {error.Message}");
        }

        throw new ChangeLogInvalidFailedException("Changelog has lint errors");
    }

    private static async Task OutputUnreleasedContentAsync(
        Options options,
        IChangeLogDetector detector,
        IChangeLogReader reader,
        CancellationToken cancellationToken
    )
    {
        string changeLog = FindChangeLog(options, detector);
        Console.WriteLine($"Using Changelog {changeLog}");

        Console.WriteLine();
        Console.WriteLine("Unreleased Content:");
        string text = await reader.ExtractReleaseNotesFromFileAsync(
            changeLogFileName: changeLog,
            version: "0.0.0.0-unreleased",
            cancellationToken: cancellationToken
        );
        Console.WriteLine(text);
    }

    private static Task CreateNewReleaseAsync(
        Options options,
        IChangeLogDetector detector,
        ChangeLogLanguage language,
        IChangeLogUpdater updater,
        in CancellationToken cancellationToken
    )
    {
        string releaseVersion = options.CreateRelease ?? string.Empty;
        string changeLog = FindChangeLog(options, detector);
        Console.WriteLine($"Using Changelog {changeLog}");
        Console.WriteLine($"Release Version: {releaseVersion}");

        return updater.CreateReleaseAsync(
            changeLogFileName: changeLog,
            language: language,
            version: releaseVersion,
            pending: options.Pending,
            cancellationToken: cancellationToken
        );
    }

    private static async Task CheckInsertPositionAsync(
        Options options,
        IChangeLogDetector detector,
        IChangeLogChecker checker,
        CancellationToken cancellationToken
    )
    {
        string originBranchName = options.CheckInsert ?? string.Empty;
        string changeLog = FindChangeLog(options, detector);
        Console.WriteLine($"Using Changelog {changeLog}");
        Console.WriteLine($"Branch: {originBranchName}");
        bool valid = await checker.ChangeLogModifiedInReleaseSectionAsync(
            changeLogFileName: changeLog,
            originBranchName: originBranchName,
            cancellationToken: cancellationToken
        );

        if (valid)
        {
            Console.WriteLine("Changelog is valid");

            return;
        }

        throw new ChangeLogInvalidFailedException("Changelog modified in released section");
    }

    private static Task AddEntryToUnreleasedChangelogAsync(
        Options options,
        IChangeLogDetector detector,
        ChangeLogLanguage language,
        IChangeLogUpdater updater,
        in CancellationToken cancellationToken
    )
    {
        string changeType = options.Add ?? string.Empty;
        string message = options.Message ?? string.Empty;
        string changeLog = FindChangeLog(options, detector);
        Console.WriteLine($"Using Changelog {changeLog}");
        Console.WriteLine($"Change Type: {changeType}");
        Console.WriteLine($"Message: {message}");

        return updater.AddEntryAsync(
            changeLogFileName: changeLog,
            language: language,
            type: changeType,
            message: message,
            cancellationToken: cancellationToken
        );
    }

    private static Task RemoveEntryFromUnreleasedChangelogAsync(
        Options options,
        IChangeLogDetector detector,
        ChangeLogLanguage language,
        IChangeLogUpdater updater,
        in CancellationToken cancellationToken
    )
    {
        string changeType = options.Remove ?? string.Empty;
        string message = options.Message ?? string.Empty;
        string changeLog = FindChangeLog(options, detector);
        Console.WriteLine($"Using Changelog {changeLog}");
        Console.WriteLine($"Change Type: {changeType}");
        Console.WriteLine($"Message: {message}");

        return updater.RemoveEntryAsync(
            changeLogFileName: changeLog,
            language: language,
            type: changeType,
            message: message,
            cancellationToken: cancellationToken
        );
    }

    private static async Task ExtractChangeLogTextForVersionAsync(
        Options options,
        IChangeLogDetector detector,
        IChangeLogReader reader,
        CancellationToken cancellationToken
    )
    {
        string outputFileName = options.Extract ?? string.Empty;
        string version = options.Version ?? string.Empty;
        string changeLog = FindChangeLog(options, detector);
        Console.WriteLine($"Using Changelog {changeLog}");
        Console.WriteLine($"Version {version}");

        string text = await reader.ExtractReleaseNotesFromFileAsync(
            changeLogFileName: changeLog,
            version: version,
            cancellationToken: cancellationToken
        );

        await File.WriteAllTextAsync(
            path: outputFileName,
            contents: text,
            encoding: Encoding.UTF8,
            cancellationToken: cancellationToken
        );
    }

    private static void NotParsed(IEnumerable<Error> errors)
    {
        Console.WriteLine("Errors:");

        foreach (Error error in errors)
        {
            Console.WriteLine($" * {error.Tag.GetName()}");
        }
    }

    [SuppressMessage(
        category: "Meziantou.Analyzer",
        checkId: "MA0109",
        Justification = "Main(string[] args) is the mandated .NET entry point signature"
    )]
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine($"{VersionInformation.Product} {VersionInformation.Version}");

        await using ServiceProvider serviceProvider = BuildServiceProvider();

        try
        {
            ParserResult<Options> parser = await ParseOptionsAsync(args, serviceProvider);

            return parser.Tag == ParserResultType.Parsed ? SUCCESS : ERROR;
        }
        catch (InvalidOptionsException exception)
        {
            Console.WriteLine($"ERROR: {exception.Message}");

            return ERROR;
        }
        catch (Exception exception)
        {
            Console.WriteLine($"ERROR: {exception.Message}");
            Console.WriteLine(exception.StackTrace);

            return ERROR;
        }
    }

    private static ServiceProvider BuildServiceProvider()
    {
        return new ServiceCollection().AddChangeLog().BuildServiceProvider();
    }

    private static Task<ParserResult<Options>> ParseOptionsAsync(
        IEnumerable<string> args,
        IServiceProvider serviceProvider
    )
    {
        return Parser
            .Default.ParseArguments<Options>(args)
            .WithNotParsed(NotParsed)
            .WithParsedAsync(options => ParsedOkAsync(options, serviceProvider));
    }
}
