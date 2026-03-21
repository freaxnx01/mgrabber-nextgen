using Microsoft.Extensions.DependencyInjection;

namespace MusicGrabber.Modules.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        return services;
    }
}
