using Microsoft.Extensions.DependencyInjection;
using MusicGrabber.Modules.Discovery.Application.Ports.Driven;
using MusicGrabber.Modules.Discovery.Application.Ports.Driving;
using MusicGrabber.Modules.Discovery.Application.Services;
using MusicGrabber.Modules.Discovery.Infrastructure.Adapters;

namespace MusicGrabber.Modules.Discovery.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDiscoveryModule(this IServiceCollection services)
    {
        services.AddMemoryCache();

        services.AddHttpClient<IMusicBrainzApi, MusicBrainzApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://musicbrainz.org/ws/2/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MusicGrabber/1.0 (personal-use)");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });

        services.AddScoped<IMusicBrainzService, MusicBrainzService>();

        return services;
    }
}
