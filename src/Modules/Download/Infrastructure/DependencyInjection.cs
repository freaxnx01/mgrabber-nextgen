using Microsoft.Extensions.DependencyInjection;

namespace MusicGrabber.Modules.Download.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDownloadModule(this IServiceCollection services)
    {
        return services;
    }
}
