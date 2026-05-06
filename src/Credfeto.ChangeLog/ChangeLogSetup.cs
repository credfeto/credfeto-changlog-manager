using Credfeto.ChangeLog.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Credfeto.ChangeLog;

public static class ChangeLogSetup
{
    public static IServiceCollection AddChangeLog(this IServiceCollection services)
    {
        return services.AddSingleton<IChangeLogStorage, FileSystemChangeLogStorage>()
                       .AddSingleton<IChangeLogParser, ChangeLogParser>()
                       .AddSingleton<IChangeLogSerialiser, ChangeLogSerialiser>()
                       .AddSingleton<IChangeLogLanguageFactory, ChangeLogLanguageFactory>()
                       .AddSingleton(sp => sp.GetRequiredService<IChangeLogLanguageFactory>().Get(ChangeLogLanguageFactory.English))
                       .AddSingleton<IChangeLogReader, ChangeLogReader>()
                       .AddSingleton<IChangeLogLinter, ChangeLogLinter>()
                       .AddSingleton<IChangeLogFixer, ChangeLogFixer>()
                       .AddSingleton<IChangeLogUpdater, ChangeLogUpdater>()
                       .AddSingleton<IChangeLogChecker, ChangeLogChecker>()
                       .AddSingleton<IChangeLogDetector, ChangeLogDetector>();
    }
}
