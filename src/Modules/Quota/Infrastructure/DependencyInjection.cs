using Microsoft.Extensions.DependencyInjection;

namespace MusicGrabber.Modules.Quota.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddQuotaModule(this IServiceCollection services)
    {
        return services;
    }
}
