using Credfeto.ChangeLog.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Credfeto.ChangeLog;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddChangeLog(this IServiceCollection services)
    {
        services.AddSingleton<IChangeLogLoader>(FileSystemChangeLogLoader.Instance);
        services.AddSingleton<IChangeLogReader>(sp => new ChangeLogReaderService(sp.GetRequiredService<IChangeLogLoader>()));
        services.AddSingleton<IChangeLogLinter>(sp => new ChangeLogLinterService(sp.GetRequiredService<IChangeLogLoader>()));
        services.AddSingleton<IChangeLogFixer>(sp => new ChangeLogFixerService(sp.GetRequiredService<IChangeLogLoader>()));
        services.AddSingleton<IChangeLogUpdater>(sp => new ChangeLogUpdaterService(sp.GetRequiredService<IChangeLogLoader>()));

        return services;
    }
}
