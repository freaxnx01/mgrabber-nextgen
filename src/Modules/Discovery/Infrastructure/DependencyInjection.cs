using Microsoft.Extensions.DependencyInjection;

namespace MusicGrabber.Modules.Discovery.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDiscoveryModule(this IServiceCollection services)
    {
        return services;
    }
}
