using Credfeto.ChangeLog.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Credfeto.ChangeLog;

public static class ChangeLogSetup
{
    public static IServiceCollection AddChangeLog(this IServiceCollection services)
    {
        return services.AddSingleton<IChangeLogLoader, FileSystemChangeLogLoader>()
                       .AddSingleton<IChangeLogReader, ChangeLogReaderService>()
                       .AddSingleton<IChangeLogLinter, ChangeLogLinterService>()
                       .AddSingleton<IChangeLogFixer, ChangeLogFixerService>()
                       .AddSingleton<IChangeLogUpdater, ChangeLogUpdaterService>();
    }
}
