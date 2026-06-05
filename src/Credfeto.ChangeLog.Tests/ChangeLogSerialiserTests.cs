using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Models;
using Credfeto.ChangeLog.Services;
using Credfeto.ChangeLog.Tests.TestHelpers;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

public sealed class ChangeLogSerialiserTests : TestBase
{
    private static readonly ChangeLogLanguage Language = new ChangeLogLanguageFactory().Get(
        ChangeLogLanguageFactory.English
    );

    [Fact]
    public async Task OrderSectionsMergesDuplicateSectionsWithSameName()
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

        ChangeLogDocument document = await ChangeLogTestHelper.ParseAsync(changeLog);
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
