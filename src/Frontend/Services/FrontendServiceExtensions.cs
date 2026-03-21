using Microsoft.Extensions.DependencyInjection;

namespace MusicGrabber.Frontend.Services;

public static class FrontendServiceExtensions
{
    public static IServiceCollection AddFrontendServices(this IServiceCollection services)
    {
        services.AddScoped<IDownloadFrontendService, DownloadFrontendService>();
        services.AddScoped<IDiscoveryFrontendService, DiscoveryFrontendService>();
        services.AddScoped<IRadioFrontendService, RadioFrontendService>();
        services.AddScoped<IQuotaFrontendService, QuotaFrontendService>();
        services.AddScoped<IIdentityFrontendService, IdentityFrontendService>();
        return services;
    }
}
