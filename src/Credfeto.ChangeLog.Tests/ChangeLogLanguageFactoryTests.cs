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

    [Fact]
    public void Get_English_ReturnsSevenSections()
    {
        ChangeLogLanguage language = Factory.Get(ChangeLogLanguageFactory.English);

        Assert.Equal(expected: 7, actual: language.SectionOrder.Length);
    }

    [Theory]
    [InlineData(ChangeLogLanguageFactory.Czech)]
    [InlineData(ChangeLogLanguageFactory.Danish)]
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
    public void Get_KnownLanguageCode_ReturnsSixSections(string languageCode)
    {
        ChangeLogLanguage language = Factory.Get(languageCode);

        Assert.Equal(expected: 6, actual: language.SectionOrder.Length);
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

    [Fact]
    public void Get_English_LastSectionIsDeploymentChanges()
    {
        ChangeLogLanguage language = Factory.Get(ChangeLogLanguageFactory.English);

        Assert.Equal(expected: "Deployment Changes", actual: language.SectionOrder[^1]);
    }

    [Theory]
    [InlineData(ChangeLogLanguageFactory.Czech, "Odebráno")]
    [InlineData(ChangeLogLanguageFactory.Danish, "Fjernet")]
    [InlineData(ChangeLogLanguageFactory.German, "Entfernt")]
    [InlineData(ChangeLogLanguageFactory.Spanish, "Eliminado")]
    [InlineData(ChangeLogLanguageFactory.French, "Supprimé")]
    [InlineData(ChangeLogLanguageFactory.Italian, "Rimosso")]
    [InlineData(ChangeLogLanguageFactory.Dutch, "Verwijderd")]
    [InlineData(ChangeLogLanguageFactory.Polish, "Usunięte")]
    [InlineData(ChangeLogLanguageFactory.BrazilianPortuguese, "Removido")]
    [InlineData(ChangeLogLanguageFactory.Russian, "Удалено")]
    [InlineData(ChangeLogLanguageFactory.Turkish, "Kaldırıldı")]
    [InlineData(ChangeLogLanguageFactory.Ukrainian, "Видалено")]
    [InlineData(ChangeLogLanguageFactory.ChineseSimplified, "移除")]
    [InlineData(ChangeLogLanguageFactory.ChineseTraditional, "移除")]
    public void Get_KnownLanguageCode_LastSectionIsRemovedEquivalent(string languageCode, string expectedLastSection)
    {
        ChangeLogLanguage language = Factory.Get(languageCode);

        Assert.Equal(expected: expectedLastSection, actual: language.SectionOrder[^1]);
    }

    [Theory]
    [InlineData(ChangeLogLanguageFactory.Czech, "Bezpečnost")]
    [InlineData(ChangeLogLanguageFactory.Danish, "Sikkerhed")]
    [InlineData(ChangeLogLanguageFactory.English, "Security")]
    [InlineData(ChangeLogLanguageFactory.German, "Sicherheit")]
    [InlineData(ChangeLogLanguageFactory.Spanish, "Seguridad")]
    [InlineData(ChangeLogLanguageFactory.French, "Sécurité")]
    [InlineData(ChangeLogLanguageFactory.Italian, "Sicurezza")]
    [InlineData(ChangeLogLanguageFactory.Dutch, "Beveiliging")]
    [InlineData(ChangeLogLanguageFactory.Polish, "Bezpieczeństwo")]
    [InlineData(ChangeLogLanguageFactory.BrazilianPortuguese, "Segurança")]
    [InlineData(ChangeLogLanguageFactory.Russian, "Безопасность")]
    [InlineData(ChangeLogLanguageFactory.Turkish, "Güvenlik")]
    [InlineData(ChangeLogLanguageFactory.Ukrainian, "Безпека")]
    [InlineData(ChangeLogLanguageFactory.ChineseSimplified, "安全性")]
    [InlineData(ChangeLogLanguageFactory.ChineseTraditional, "安全性")]
    public void Get_KnownLanguageCode_FirstSectionIsSecurityEquivalent(string languageCode, string expectedFirstSection)
    {
        ChangeLogLanguage language = Factory.Get(languageCode);

        Assert.Equal(expected: expectedFirstSection, actual: language.SectionOrder[0]);
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

    [Fact]
    public void Get_Czech_HasExpectedUnreleasedSectionName()
    {
        ChangeLogLanguage language = Factory.Get(ChangeLogLanguageFactory.Czech);

        Assert.Equal(expected: "Nevydáno", actual: language.UnreleasedSectionName);
    }

    [Fact]
    public void Get_Danish_HasExpectedUnreleasedSectionName()
    {
        ChangeLogLanguage language = Factory.Get(ChangeLogLanguageFactory.Danish);

        Assert.Equal(expected: "Ikke frigivet", actual: language.UnreleasedSectionName);
    }

    [Fact]
    public void Get_German_HasExpectedUnreleasedSectionName()
    {
        ChangeLogLanguage language = Factory.Get(ChangeLogLanguageFactory.German);

        Assert.Equal(expected: "Unveröffentlicht", actual: language.UnreleasedSectionName);
    }

    [Fact]
    public void Get_Spanish_HasExpectedUnreleasedSectionName()
    {
        ChangeLogLanguage language = Factory.Get(ChangeLogLanguageFactory.Spanish);

        Assert.Equal(expected: "Sin publicar", actual: language.UnreleasedSectionName);
    }

    [Fact]
    public void Get_French_HasExpectedUnreleasedSectionName()
    {
        ChangeLogLanguage language = Factory.Get(ChangeLogLanguageFactory.French);

        Assert.Equal(expected: "Non publié", actual: language.UnreleasedSectionName);
    }

    [Fact]
    public void Get_Italian_HasExpectedUnreleasedSectionName()
    {
        ChangeLogLanguage language = Factory.Get(ChangeLogLanguageFactory.Italian);

        Assert.Equal(expected: "Non pubblicato", actual: language.UnreleasedSectionName);
    }

    [Fact]
    public void Get_Dutch_HasExpectedUnreleasedSectionName()
    {
        ChangeLogLanguage language = Factory.Get(ChangeLogLanguageFactory.Dutch);

        Assert.Equal(expected: "Niet gepubliceerd", actual: language.UnreleasedSectionName);
    }

    [Fact]
    public void Get_ChineseSimplified_HasExpectedUnreleasedSectionName()
    {
        ChangeLogLanguage language = Factory.Get(ChangeLogLanguageFactory.ChineseSimplified);

        Assert.Equal(expected: "未发布", actual: language.UnreleasedSectionName);
    }

    [Fact]
    public void Get_ChineseTraditional_HasExpectedUnreleasedSectionName()
    {
        ChangeLogLanguage language = Factory.Get(ChangeLogLanguageFactory.ChineseTraditional);

        Assert.Equal(expected: "未發布", actual: language.UnreleasedSectionName);
    }
}
