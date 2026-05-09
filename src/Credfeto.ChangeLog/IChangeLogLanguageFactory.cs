namespace Credfeto.ChangeLog;

public interface IChangeLogLanguageFactory
{
    ChangeLogLanguage Get(string languageCode);
}
