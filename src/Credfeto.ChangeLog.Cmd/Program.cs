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

internal static class Program
{
    private const int SUCCESS = 0;
    private const int ERROR = 1;

    private static string FindChangeLog(Options options)
    {
        string? changeLog = options.ChangeLog;

        if (changeLog is not null)
        {
            return changeLog;
        }

        if (ChangeLogDetector.TryFindChangeLog(out changeLog))
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

        if (options.Extract is not null && options.Version is not null)
        {
            await ExtractChangeLogTextForVersionAsync(options: options, reader: services.GetRequiredService<IChangeLogReader>(), cancellationToken: cancellationToken);

            return;
        }

        if (options.Add is not null && options.Message is not null)
        {
            await AddEntryToUnreleasedChangelogAsync(options: options, updater: services.GetRequiredService<IChangeLogUpdater>(), cancellationToken: cancellationToken);

            return;
        }

        if (options.Remove is not null && options.Message is not null)
        {
            await RemoveEntryFromUnreleasedChangelogAsync(options: options, updater: services.GetRequiredService<IChangeLogUpdater>(), cancellationToken: cancellationToken);

            return;
        }

        await ParsedOkContinuationAsync(options: options, services: services, cancellationToken: cancellationToken);
    }

    private static async Task ParsedOkContinuationAsync(Options options, IServiceProvider services, CancellationToken cancellationToken)
    {
        if (options.CheckInsert is not null)
        {
            await CheckInsertPositionAsync(options: options, loader: services.GetRequiredService<IChangeLogLoader>(), cancellationToken: cancellationToken);

            return;
        }

        if (options.CreateRelease is not null)
        {
            await CreateNewReleaseAsync(options: options, updater: services.GetRequiredService<IChangeLogUpdater>(), cancellationToken: cancellationToken);

            return;
        }

        if (options.DisplayUnreleased)
        {
            await OutputUnreleasedContentAsync(options: options, reader: services.GetRequiredService<IChangeLogReader>(), cancellationToken: cancellationToken);

            return;
        }

        if (options.Lint)
        {
            await LintChangeLogAsync(options: options, linter: services.GetRequiredService<IChangeLogLinter>(), fixer: services.GetRequiredService<IChangeLogFixer>(), cancellationToken: cancellationToken);

            return;
        }

        throw new InvalidOptionsException();
    }

    private static async Task LintChangeLogAsync(Options options, IChangeLogLinter linter, IChangeLogFixer fixer, CancellationToken cancellationToken)
    {
        string changeLog = FindChangeLog(options);
        Console.WriteLine($"Using Changelog {changeLog}");

        IReadOnlyList<string> additionalSections = [.. options.AdditionalSections];
        IReadOnlyCollection<string>? additionalSectionsArg = additionalSections.Count > 0 ? additionalSections : null;

        IReadOnlyList<LintError> errors = await linter.LintFileAsync(
            changeLogFileName: changeLog,
            additionalSections: additionalSectionsArg,
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
            Console.WriteLine("Applying fixes...");

            await fixer.FixFileAsync(
                changeLogFileName: changeLog,
                additionalSections: additionalSectionsArg,
                cancellationToken: cancellationToken
            );

            Console.WriteLine("Fixed. Re-linting...");

            IReadOnlyList<LintError> remainingErrors = await linter.LintFileAsync(
                changeLogFileName: changeLog,
                additionalSections: additionalSectionsArg,
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
        }

        throw new ChangeLogInvalidFailedException("Changelog has lint errors");
    }

    private static async Task OutputUnreleasedContentAsync(Options options, IChangeLogReader reader, CancellationToken cancellationToken)
    {
        string changeLog = FindChangeLog(options);
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

    private static Task CreateNewReleaseAsync(Options options, IChangeLogUpdater updater, in CancellationToken cancellationToken)
    {
        string releaseVersion = GetCreateRelease(options);
        string changeLog = FindChangeLog(options);
        Console.WriteLine($"Using Changelog {changeLog}");
        Console.WriteLine($"Release Version: {releaseVersion}");

        return updater.CreateReleaseAsync(
            changeLogFileName: changeLog,
            version: releaseVersion,
            pending: options.Pending,
            cancellationToken: cancellationToken
        );
    }

    private static string GetCreateRelease(Options options)
    {
        return options.CreateRelease ?? throw new InvalidOptionsException(nameof(options.CreateRelease) + " is null");
    }

    private static async Task CheckInsertPositionAsync(Options options, IChangeLogLoader loader, CancellationToken cancellationToken)
    {
        string originBranchName = GetCheckInsert(options);
        string changeLog = FindChangeLog(options);
        Console.WriteLine($"Using Changelog {changeLog}");
        Console.WriteLine($"Branch: {originBranchName}");
        bool valid = await ChangeLogChecker.ChangeLogModifiedInReleaseSectionAsync(
            changeLogFileName: changeLog,
            originBranchName: originBranchName,
            loader: loader,
            cancellationToken: cancellationToken
        );

        if (valid)
        {
            Console.WriteLine("Changelog is valid");

            return;
        }

        throw new ChangeLogInvalidFailedException("Changelog modified in released section");
    }

    private static string GetCheckInsert(Options options)
    {
        return options.CheckInsert ?? throw new InvalidOptionsException(nameof(options.CheckInsert) + " is null");
    }

    private static Task AddEntryToUnreleasedChangelogAsync(Options options, IChangeLogUpdater updater, in CancellationToken cancellationToken)
    {
        string changeType = GetAdd(options);
        string message = GetMessage(options);
        string changeLog = FindChangeLog(options);
        Console.WriteLine($"Using Changelog {changeLog}");
        Console.WriteLine($"Change Type: {changeType}");
        Console.WriteLine($"Message: {message}");

        return updater.AddEntryAsync(
            changeLogFileName: changeLog,
            type: changeType,
            message: message,
            cancellationToken: cancellationToken
        );
    }

    private static string GetAdd(Options options)
    {
        return options.Add ?? throw new InvalidOptionsException(nameof(options.Add) + " is null");
    }

    private static Task RemoveEntryFromUnreleasedChangelogAsync(Options options, IChangeLogUpdater updater, in CancellationToken cancellationToken)
    {
        string changeType = GetChangeType(options);
        string message = GetMessage(options);
        string changeLog = FindChangeLog(options);
        Console.WriteLine($"Using Changelog {changeLog}");
        Console.WriteLine($"Change Type: {changeType}");
        Console.WriteLine($"Message: {message}");

        return updater.RemoveEntryAsync(
            changeLogFileName: changeLog,
            type: changeType,
            message: message,
            cancellationToken: cancellationToken
        );
    }

    private static string GetChangeType(Options options)
    {
        return options.Remove ?? throw new InvalidOptionsException(nameof(options.Remove) + " is null");
    }

    private static string GetMessage(Options options)
    {
        return options.Message ?? throw new InvalidOptionsException(nameof(options.Message) + " is null");
    }

    private static async Task ExtractChangeLogTextForVersionAsync(Options options, IChangeLogReader reader, CancellationToken cancellationToken)
    {
        string outputFileName = GetExtract(options);
        string version = GetVersion(options);
        string changeLog = FindChangeLog(options);
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

    private static string GetVersion(Options options)
    {
        return options.Version ?? throw new InvalidOptionsException(nameof(options.Version) + " is null");
    }

    private static string GetExtract(Options options)
    {
        return options.Extract ?? throw new InvalidOptionsException(nameof(options.Extract) + " is null");
    }

    private static void NotParsed(IEnumerable<Error> errors)
    {
        Console.WriteLine("Errors:");

        foreach (Error error in errors)
        {
            Console.WriteLine($" * {error.Tag.GetName()}");
        }
    }

    private static async Task<int> Main(string[] args)
    {
        Console.WriteLine($"{VersionInformation.Product} {VersionInformation.Version}");

        try
        {
            await using (ServiceProvider serviceProvider = BuildServiceProvider())
            {

                ParserResult<Options> parser = await ParseOptionsAsync(args, serviceProvider);

                return parser.Tag == ParserResultType.Parsed
                    ? SUCCESS
                    : ERROR;
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine($"ERROR: {exception.Message}");

            if (exception.StackTrace is not null)
            {
                Console.WriteLine(exception.StackTrace);
            }

            return ERROR;
        }
    }

    private static ServiceProvider BuildServiceProvider()
    {
        return new ServiceCollection().AddChangeLog()
                                      .BuildServiceProvider();
    }

    private static Task<ParserResult<Options>> ParseOptionsAsync(IEnumerable<string> args, IServiceProvider serviceProvider)
    {
        return Parser.Default.ParseArguments<Options>(args)
            .WithNotParsed(NotParsed)
            .WithParsedAsync(options => ParsedOkAsync(options, serviceProvider));
    }
}
