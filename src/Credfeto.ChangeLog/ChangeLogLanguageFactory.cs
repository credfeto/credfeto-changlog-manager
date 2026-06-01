using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using Credfeto.ChangeLog.Helpers;

namespace Credfeto.ChangeLog;

public sealed class ChangeLogLanguageFactory : IChangeLogLanguageFactory
{
    public const string Czech = "cs";
    public const string Danish = "da";
    public const string English = "en";
    public const string German = "de";
    public const string Spanish = "es";
    public const string French = "fr";
    public const string Italian = "it";
    public const string Dutch = "nl";
    public const string Polish = "pl";
    public const string BrazilianPortuguese = "pt-BR";
    public const string Russian = "ru";
    public const string Turkish = "tr";
    public const string Ukrainian = "uk";
    public const string ChineseSimplified = "zh-CN";
    public const string ChineseTraditional = "zh-TW";

    private static readonly FrozenDictionary<string, ChangeLogLanguage> Languages = new Dictionary<
        string,
        ChangeLogLanguage
    >(StringComparer.Ordinal)
    {
        [Czech] = new(
            DocumentTitle: "Changelog",
            UnreleasedSectionName: "Unreleased",
            SectionOrder: ChangeLogSections.Order,
            DateFormat: "yyyy-MM-dd"
        ),
        [Danish] = new(
            DocumentTitle: "Changelog",
            UnreleasedSectionName: "Unreleased",
            SectionOrder: ["Sikkerhed", "Tilføjet", "Rettet", "Ændret", "Udfaset", "Fjernet", "Deployment Changes"],
            DateFormat: "yyyy-MM-dd"
        ),
        [English] = new(
            DocumentTitle: "Changelog",
            UnreleasedSectionName: "Unreleased",
            SectionOrder: ChangeLogSections.Order,
            DateFormat: "yyyy-MM-dd"
        ),
        [German] = new(
            DocumentTitle: "CHANGELOG",
            UnreleasedSectionName: "Unreleased",
            SectionOrder: ChangeLogSections.Order,
            DateFormat: "yyyy-MM-dd"
        ),
        [Spanish] = new(
            DocumentTitle: "Changelog",
            UnreleasedSectionName: "Unreleased",
            SectionOrder: ChangeLogSections.Order,
            DateFormat: "yyyy-MM-dd"
        ),
        [French] = new(
            DocumentTitle: "Changelog",
            UnreleasedSectionName: "Unreleased",
            SectionOrder: ChangeLogSections.Order,
            DateFormat: "yyyy-MM-dd"
        ),
        [Italian] = new(
            DocumentTitle: "Changelog",
            UnreleasedSectionName: "Unreleased",
            SectionOrder: ChangeLogSections.Order,
            DateFormat: "yyyy-MM-dd"
        ),
        [Dutch] = new(
            DocumentTitle: "Changelog",
            UnreleasedSectionName: "Unreleased",
            SectionOrder: ChangeLogSections.Order,
            DateFormat: "yyyy-MM-dd"
        ),
        [Polish] = new(
            DocumentTitle: "Changelog",
            UnreleasedSectionName: "Niewydane",
            SectionOrder:
            [
                "Bezpieczeństwo",
                "Dodane",
                "Naprawione",
                "Zmienione",
                "Zdezaprobowane",
                "Usunięte",
                "Deployment Changes",
            ],
            DateFormat: "yyyy-MM-dd"
        ),
        [BrazilianPortuguese] = new(
            DocumentTitle: "Changelog",
            UnreleasedSectionName: "Não publicado",
            SectionOrder:
            [
                "Segurança",
                "Adicionado",
                "Corrigido",
                "Modificado",
                "Obsoleto",
                "Removido",
                "Deployment Changes",
            ],
            DateFormat: "yyyy-MM-dd"
        ),
        [Russian] = new(
            DocumentTitle: "Лог изменений",
            UnreleasedSectionName: "Новое",
            SectionOrder:
            [
                "Безопасность",
                "Добавлено",
                "Исправлено",
                "Изменено",
                "Устарело",
                "Удалено",
                "Deployment Changes",
            ],
            DateFormat: "yyyy-MM-dd"
        ),
        [Turkish] = new(
            DocumentTitle: "Değişiklik kaydı",
            UnreleasedSectionName: "Yayımlanmadı",
            SectionOrder:
            [
                "Güvenlik",
                "Eklendi",
                "Düzeltildi",
                "Değişti",
                "Rafa kalktı",
                "Kaldırıldı",
                "Deployment Changes",
            ],
            DateFormat: "yyyy-MM-dd"
        ),
        [Ukrainian] = new(
            DocumentTitle: "Лог змін",
            UnreleasedSectionName: "Нове",
            SectionOrder:
            [
                "Безпека",
                "Додано",
                "Виправлення",
                "Змінено",
                "Застаріло",
                "Видалено",
                "Deployment Changes",
            ],
            DateFormat: "yyyy-MM-dd"
        ),
        [ChineseSimplified] = new(
            DocumentTitle: "更新日志",
            UnreleasedSectionName: "Unreleased",
            SectionOrder: ChangeLogSections.Order,
            DateFormat: "yyyy-MM-dd"
        ),
        [ChineseTraditional] = new(
            DocumentTitle: "更新日誌",
            UnreleasedSectionName: "Unreleased",
            SectionOrder: ChangeLogSections.Order,
            DateFormat: "yyyy-MM-dd"
        ),
    }.ToFrozenDictionary(StringComparer.Ordinal);

    public ChangeLogLanguage Get(string languageCode)
    {
        return Languages.TryGetValue(languageCode, out ChangeLogLanguage? language)
            ? language
            : throw new ArgumentException(
                message: $"Unknown language code: {languageCode}",
                paramName: nameof(languageCode)
            );
    }
}
