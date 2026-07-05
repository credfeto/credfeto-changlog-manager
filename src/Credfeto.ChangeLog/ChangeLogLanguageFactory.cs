using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;

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

    internal static readonly ImmutableArray<string> DefaultSectionOrder =
    [
        "Security",
        "Added",
        "Fixed",
        "Changed",
        "Deprecated",
        "Removed",
        "Deployment Changes",
    ];

    private static readonly FrozenDictionary<string, ChangeLogLanguage> Languages = new Dictionary<
        string,
        ChangeLogLanguage
    >(StringComparer.Ordinal)
    {
        [Czech] = new(
            DocumentTitle: "Changelog",
            UnreleasedSectionName: "Nevydáno",
            SectionOrder: ["Bezpečnost", "Přidáno", "Opraveno", "Změněno", "Zastaralé", "Odebráno"],
            DateFormat: "yyyy-MM-dd"
        ),
        [Danish] = new(
            DocumentTitle: "Changelog",
            UnreleasedSectionName: "Ikke frigivet",
            SectionOrder: ["Sikkerhed", "Tilføjet", "Rettet", "Ændret", "Udfaset", "Fjernet"],
            DateFormat: "yyyy-MM-dd"
        ),
        [English] = new(
            DocumentTitle: "Changelog",
            UnreleasedSectionName: "Unreleased",
            SectionOrder: DefaultSectionOrder,
            DateFormat: "yyyy-MM-dd"
        ),
        [German] = new(
            DocumentTitle: "CHANGELOG",
            UnreleasedSectionName: "Unveröffentlicht",
            SectionOrder: ["Sicherheit", "Hinzugefügt", "Behoben", "Geändert", "Veraltet", "Entfernt"],
            DateFormat: "yyyy-MM-dd"
        ),
        [Spanish] = new(
            DocumentTitle: "Changelog",
            UnreleasedSectionName: "Sin publicar",
            SectionOrder: ["Seguridad", "Añadido", "Corregido", "Cambiado", "Obsoleto", "Eliminado"],
            DateFormat: "yyyy-MM-dd"
        ),
        [French] = new(
            DocumentTitle: "Changelog",
            UnreleasedSectionName: "Non publié",
            SectionOrder: ["Sécurité", "Ajouté", "Corrigé", "Modifié", "Déprécié", "Supprimé"],
            DateFormat: "yyyy-MM-dd"
        ),
        [Italian] = new(
            DocumentTitle: "Changelog",
            UnreleasedSectionName: "Non pubblicato",
            SectionOrder: ["Sicurezza", "Aggiunto", "Corretto", "Modificato", "Deprecato", "Rimosso"],
            DateFormat: "yyyy-MM-dd"
        ),
        [Dutch] = new(
            DocumentTitle: "Changelog",
            UnreleasedSectionName: "Niet gepubliceerd",
            SectionOrder: ["Beveiliging", "Toegevoegd", "Opgelost", "Gewijzigd", "Verouderd", "Verwijderd"],
            DateFormat: "yyyy-MM-dd"
        ),
        [Polish] = new(
            DocumentTitle: "Changelog",
            UnreleasedSectionName: "Niewydane",
            SectionOrder: ["Bezpieczeństwo", "Dodane", "Naprawione", "Zmienione", "Zdezaprobowane", "Usunięte"],
            DateFormat: "yyyy-MM-dd"
        ),
        [BrazilianPortuguese] = new(
            DocumentTitle: "Changelog",
            UnreleasedSectionName: "Não publicado",
            SectionOrder: ["Segurança", "Adicionado", "Corrigido", "Modificado", "Obsoleto", "Removido"],
            DateFormat: "yyyy-MM-dd"
        ),
        [Russian] = new(
            DocumentTitle: "Лог изменений",
            UnreleasedSectionName: "Новое",
            SectionOrder: ["Безопасность", "Добавлено", "Исправлено", "Изменено", "Устарело", "Удалено"],
            DateFormat: "yyyy-MM-dd"
        ),
        [Turkish] = new(
            DocumentTitle: "Değişiklik kaydı",
            UnreleasedSectionName: "Yayımlanmadı",
            SectionOrder: ["Güvenlik", "Eklendi", "Düzeltildi", "Değişti", "Rafa kalktı", "Kaldırıldı"],
            DateFormat: "yyyy-MM-dd"
        ),
        [Ukrainian] = new(
            DocumentTitle: "Лог змін",
            UnreleasedSectionName: "Нове",
            SectionOrder: ["Безпека", "Додано", "Виправлення", "Змінено", "Застаріло", "Видалено"],
            DateFormat: "yyyy-MM-dd"
        ),
        [ChineseSimplified] = new(
            DocumentTitle: "更新日志",
            UnreleasedSectionName: "未发布",
            SectionOrder: ["安全性", "新增", "修复", "变更", "废弃", "移除"],
            DateFormat: "yyyy-MM-dd"
        ),
        [ChineseTraditional] = new(
            DocumentTitle: "更新日誌",
            UnreleasedSectionName: "未發布",
            SectionOrder: ["安全性", "新增", "修正", "變更", "廢棄", "移除"],
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
