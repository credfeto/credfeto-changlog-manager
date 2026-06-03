using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Credfeto.ChangeLog.Models;
using Credfeto.ChangeLog.Services;
using Credfeto.ChangeLog.Tests.TestHelpers;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

[SuppressMessage(
    category: "Meziantou.Analyzer",
    checkId: "MA0045:Use async overload",
    Justification = "Helpers synchronously wrap pure parse/serialise ValueTasks"
)]
[SuppressMessage(
    category: "Microsoft.VisualStudio.Threading.Analyzers",
    checkId: "VSTHRD002",
    Justification = "Helpers synchronously wrap pure parse/serialise ValueTasks"
)]
[SuppressMessage(
    category: "Microsoft.Reliability",
    checkId: "CA2012:UseValueTasksCorrectly",
    Justification = "Helpers synchronously wrap pure parse/serialise ValueTasks"
)]
public sealed class ChangeLogSerialiserTests : TestBase
{
    private static readonly ChangeLogLanguage Language = new ChangeLogLanguageFactory().Get(
        ChangeLogLanguageFactory.English
    );

    [Fact]
    public void OrderSectionsMergesDuplicateSectionsWithSameName()
    {
        const string changeLog = """
            # Changelog

            ## [Unreleased]
            ### Added
            - First added
            ### Added
            - Duplicate added
            ### Fixed
            ### Changed
            ### Removed

            ## [0.0.0] - Project created
            """;

        ChangeLogDocument document = ChangeLogTestHelper.Parse(changeLog);
        ImmutableArray<ChangeLogSection> sections = document.Unreleased?.Sections ?? [];

        // OrderSections will merge duplicate "Added" sections
        ImmutableArray<ChangeLogSection> ordered = ChangeLogSerialiser.OrderSections(
            sections: sections,
            sectionOrder: Language.SectionOrder
        );

        // After merging, there should be at most one "Added" section
        int addedCount = 0;

        foreach (ChangeLogSection s in ordered)
        {
            if (StringComparer.Ordinal.Equals(s.Name, "Added"))
            {
                addedCount++;
            }
        }

        Assert.Equal(expected: 1, actual: addedCount);
    }
}
