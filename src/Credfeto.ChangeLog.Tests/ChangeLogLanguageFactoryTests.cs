using System;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.ChangeLog.Tests;

public sealed class ChangeLogLanguageFactoryTests : TestBase
{
    private static readonly ChangeLogLanguageFactory Factory = new();

    [Theory]
    [InlineData(ChangeLogLanguageFactory.Czech)]
    [InlineData(ChangeLogLanguageFactory.Danish)]
    [InlineData(ChangeLogLanguageFactory.English)]
    [InlineData(ChangeLogLanguageFactory.German)]
    [InlineData(ChangeLogLanguageFactory.Spanish)]
    [InlineData(ChangeLogLanguageFactory.French)]
    [InlineData(ChangeLogLanguageFactory.Italian)]
    [InlineData(ChangeLogLanguageFactory.Dutch)]
    [InlineData(ChangeLogLanguageFactory.Polish)]
    [InlineData(ChangeLogLanguageFactory.BrazilianPortuguese)]
    [InlineData(ChangeLogLanguageFactory.Russian)]
    [InlineData(ChangeLogLanguageFactory.Turkish)]
    [InlineData(ChangeLogLanguageFactory.Ukrainian)]
    [InlineData(ChangeLogLanguageFactory.ChineseSimplified)]
    [InlineData(ChangeLogLanguageFactory.ChineseTraditional)]
    public void Get_KnownLanguageCode_ReturnsNonNullLanguage(string languageCode)
    {
        ChangeLogLanguage language = Factory.Get(languageCode);

        Assert.NotNull(language);
    }

    [Theory]
    [InlineData(ChangeLogLanguageFactory.Czech)]
    [InlineData(ChangeLogLanguageFactory.Danish)]
    [InlineData(ChangeLogLanguageFactory.English)]
    [InlineData(ChangeLogLanguageFactory.German)]
    [InlineData(ChangeLogLanguageFactory.Spanish)]
    [InlineData(ChangeLogLanguageFactory.French)]
    [InlineData(ChangeLogLanguageFactory.Italian)]
    [InlineData(ChangeLogLanguageFactory.Dutch)]
    [InlineData(ChangeLogLanguageFactory.Polish)]
    [InlineData(ChangeLogLanguageFactory.BrazilianPortuguese)]
    [InlineData(ChangeLogLanguageFactory.Russian)]
    [InlineData(ChangeLogLanguageFactory.Turkish)]
    [InlineData(ChangeLogLanguageFactory.Ukrainian)]
    [InlineData(ChangeLogLanguageFactory.ChineseSimplified)]
    [InlineData(ChangeLogLanguageFactory.ChineseTraditional)]
    public void Get_KnownLanguageCode_ReturnsNonEmptyDocumentTitle(string languageCode)
    {
        ChangeLogLanguage language = Factory.Get(languageCode);

        Assert.False(
            string.IsNullOrWhiteSpace(language.DocumentTitle),
            userMessage: $"Expected non-empty DocumentTitle for language '{languageCode}'"
        );
    }

    [Theory]
    [InlineData(ChangeLogLanguageFactory.Czech)]
    [InlineData(ChangeLogLanguageFactory.Danish)]
    [InlineData(ChangeLogLanguageFactory.English)]
    [InlineData(ChangeLogLanguageFactory.German)]
    [InlineData(ChangeLogLanguageFactory.Spanish)]
    [InlineData(ChangeLogLanguageFactory.French)]
    [InlineData(ChangeLogLanguageFactory.Italian)]
    [InlineData(ChangeLogLanguageFactory.Dutch)]
    [InlineData(ChangeLogLanguageFactory.Polish)]
    [InlineData(ChangeLogLanguageFactory.BrazilianPortuguese)]
    [InlineData(ChangeLogLanguageFactory.Russian)]
    [InlineData(ChangeLogLanguageFactory.Turkish)]
    [InlineData(ChangeLogLanguageFactory.Ukrainian)]
    [InlineData(ChangeLogLanguageFactory.ChineseSimplified)]
    [InlineData(ChangeLogLanguageFactory.ChineseTraditional)]
    public void Get_KnownLanguageCode_ReturnsNonEmptyUnreleasedSectionName(string languageCode)
    {
        ChangeLogLanguage language = Factory.Get(languageCode);

        Assert.False(
            string.IsNullOrWhiteSpace(language.UnreleasedSectionName),
            userMessage: $"Expected non-empty UnreleasedSectionName for language '{languageCode}'"
        );
    }

    [Theory]
    [InlineData(ChangeLogLanguageFactory.Czech)]
    [InlineData(ChangeLogLanguageFactory.Danish)]
    [InlineData(ChangeLogLanguageFactory.English)]
    [InlineData(ChangeLogLanguageFactory.German)]
    [InlineData(ChangeLogLanguageFactory.Spanish)]
    [InlineData(ChangeLogLanguageFactory.French)]
    [InlineData(ChangeLogLanguageFactory.Italian)]
    [InlineData(ChangeLogLanguageFactory.Dutch)]
    [InlineData(ChangeLogLanguageFactory.Polish)]
    [InlineData(ChangeLogLanguageFactory.BrazilianPortuguese)]
    [InlineData(ChangeLogLanguageFactory.Russian)]
    [InlineData(ChangeLogLanguageFactory.Turkish)]
    [InlineData(ChangeLogLanguageFactory.Ukrainian)]
    [InlineData(ChangeLogLanguageFactory.ChineseSimplified)]
    [InlineData(ChangeLogLanguageFactory.ChineseTraditional)]
    public void Get_KnownLanguageCode_ReturnsSevenSections(string languageCode)
    {
        ChangeLogLanguage language = Factory.Get(languageCode);

        Assert.Equal(expected: 7, actual: language.SectionOrder.Length);
    }

    [Theory]
    [InlineData(ChangeLogLanguageFactory.Czech)]
    [InlineData(ChangeLogLanguageFactory.Danish)]
    [InlineData(ChangeLogLanguageFactory.English)]
    [InlineData(ChangeLogLanguageFactory.German)]
    [InlineData(ChangeLogLanguageFactory.Spanish)]
    [InlineData(ChangeLogLanguageFactory.French)]
    [InlineData(ChangeLogLanguageFactory.Italian)]
    [InlineData(ChangeLogLanguageFactory.Dutch)]
    [InlineData(ChangeLogLanguageFactory.Polish)]
    [InlineData(ChangeLogLanguageFactory.BrazilianPortuguese)]
    [InlineData(ChangeLogLanguageFactory.Russian)]
    [InlineData(ChangeLogLanguageFactory.Turkish)]
    [InlineData(ChangeLogLanguageFactory.Ukrainian)]
    [InlineData(ChangeLogLanguageFactory.ChineseSimplified)]
    [InlineData(ChangeLogLanguageFactory.ChineseTraditional)]
    public void Get_KnownLanguageCode_AllSectionsNonEmpty(string languageCode)
    {
        ChangeLogLanguage language = Factory.Get(languageCode);

        foreach (string section in language.SectionOrder)
        {
            Assert.False(
                string.IsNullOrWhiteSpace(section),
                userMessage: $"Section name must not be empty for language '{languageCode}'"
            );
        }
    }

    [Theory]
    [InlineData(ChangeLogLanguageFactory.Czech)]
    [InlineData(ChangeLogLanguageFactory.Danish)]
    [InlineData(ChangeLogLanguageFactory.English)]
    [InlineData(ChangeLogLanguageFactory.German)]
    [InlineData(ChangeLogLanguageFactory.Spanish)]
    [InlineData(ChangeLogLanguageFactory.French)]
    [InlineData(ChangeLogLanguageFactory.Italian)]
    [InlineData(ChangeLogLanguageFactory.Dutch)]
    [InlineData(ChangeLogLanguageFactory.Polish)]
    [InlineData(ChangeLogLanguageFactory.BrazilianPortuguese)]
    [InlineData(ChangeLogLanguageFactory.Russian)]
    [InlineData(ChangeLogLanguageFactory.Turkish)]
    [InlineData(ChangeLogLanguageFactory.Ukrainian)]
    [InlineData(ChangeLogLanguageFactory.ChineseSimplified)]
    [InlineData(ChangeLogLanguageFactory.ChineseTraditional)]
    public void Get_KnownLanguageCode_DateFormatIsIso8601(string languageCode)
    {
        ChangeLogLanguage language = Factory.Get(languageCode);

        Assert.Equal(expected: "yyyy-MM-dd", actual: language.DateFormat);
    }

    [Theory]
    [InlineData(ChangeLogLanguageFactory.Czech)]
    [InlineData(ChangeLogLanguageFactory.Danish)]
    [InlineData(ChangeLogLanguageFactory.English)]
    [InlineData(ChangeLogLanguageFactory.German)]
    [InlineData(ChangeLogLanguageFactory.Spanish)]
    [InlineData(ChangeLogLanguageFactory.French)]
    [InlineData(ChangeLogLanguageFactory.Italian)]
    [InlineData(ChangeLogLanguageFactory.Dutch)]
    [InlineData(ChangeLogLanguageFactory.Polish)]
    [InlineData(ChangeLogLanguageFactory.BrazilianPortuguese)]
    [InlineData(ChangeLogLanguageFactory.Russian)]
    [InlineData(ChangeLogLanguageFactory.Turkish)]
    [InlineData(ChangeLogLanguageFactory.Ukrainian)]
    [InlineData(ChangeLogLanguageFactory.ChineseSimplified)]
    [InlineData(ChangeLogLanguageFactory.ChineseTraditional)]
    public void Get_KnownLanguageCode_LastSectionIsDeploymentChanges(string languageCode)
    {
        ChangeLogLanguage language = Factory.Get(languageCode);

        Assert.Equal(expected: "Deployment Changes", actual: language.SectionOrder[^1]);
    }

    [Fact]
    public void Get_UnknownLanguageCode_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Factory.Get("xx"));
    }

    [Fact]
    public void Get_EmptyLanguageCode_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Factory.Get(string.Empty));
    }

    [Theory]
    [InlineData("EN")]
    [InlineData("En")]
    [InlineData("DE")]
    [InlineData("FR")]
    public void Get_LanguageCodeWrongCase_ThrowsArgumentException(string languageCode)
    {
        Assert.Throws<ArgumentException>(() => Factory.Get(languageCode));
    }

    [Fact]
    public void Get_English_HasExpectedDocumentTitle()
    {
        ChangeLogLanguage language = Factory.Get(ChangeLogLanguageFactory.English);

        Assert.Equal(expected: "Changelog", actual: language.DocumentTitle);
    }

    [Fact]
    public void Get_English_HasExpectedUnreleasedSectionName()
    {
        ChangeLogLanguage language = Factory.Get(ChangeLogLanguageFactory.English);

        Assert.Equal(expected: "Unreleased", actual: language.UnreleasedSectionName);
    }

    [Fact]
    public void Get_Russian_HasExpectedDocumentTitle()
    {
        ChangeLogLanguage language = Factory.Get(ChangeLogLanguageFactory.Russian);

        Assert.Equal(expected: "Лог изменений", actual: language.DocumentTitle);
    }

    [Fact]
    public void Get_Russian_HasExpectedUnreleasedSectionName()
    {
        ChangeLogLanguage language = Factory.Get(ChangeLogLanguageFactory.Russian);

        Assert.Equal(expected: "Новое", actual: language.UnreleasedSectionName);
    }

    [Fact]
    public void Get_ChineseSimplified_HasExpectedDocumentTitle()
    {
        ChangeLogLanguage language = Factory.Get(ChangeLogLanguageFactory.ChineseSimplified);

        Assert.Equal(expected: "更新日志", actual: language.DocumentTitle);
    }

    [Fact]
    public void Get_ChineseTraditional_HasExpectedDocumentTitle()
    {
        ChangeLogLanguage language = Factory.Get(ChangeLogLanguageFactory.ChineseTraditional);

        Assert.Equal(expected: "更新日誌", actual: language.DocumentTitle);
    }

    [Fact]
    public void Get_German_HasExpectedDocumentTitle()
    {
        ChangeLogLanguage language = Factory.Get(ChangeLogLanguageFactory.German);

        Assert.Equal(expected: "CHANGELOG", actual: language.DocumentTitle);
    }

    [Fact]
    public void Get_Polish_HasExpectedUnreleasedSectionName()
    {
        ChangeLogLanguage language = Factory.Get(ChangeLogLanguageFactory.Polish);

        Assert.Equal(expected: "Niewydane", actual: language.UnreleasedSectionName);
    }

    [Fact]
    public void Get_BrazilianPortuguese_HasExpectedUnreleasedSectionName()
    {
        ChangeLogLanguage language = Factory.Get(ChangeLogLanguageFactory.BrazilianPortuguese);

        Assert.Equal(expected: "Não publicado", actual: language.UnreleasedSectionName);
    }
}
