using Microsoft.Extensions.DependencyInjection;

namespace Credfeto.ChangeLog;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddChangeLog(this IServiceCollection services)
    {
        services.AddSingleton<IChangeLogLoader>(FileSystemChangeLogLoader.Instance);

        return services;
    }
}
