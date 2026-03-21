using Microsoft.Extensions.DependencyInjection;
using MusicGrabber.Modules.Radio.Application.Ports.Driven;
using MusicGrabber.Modules.Radio.Application.Ports.Driving;
using MusicGrabber.Modules.Radio.Application.Services;
using MusicGrabber.Modules.Radio.Infrastructure.Adapters;

namespace MusicGrabber.Modules.Radio.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddRadioModule(this IServiceCollection services)
    {
        services.AddMemoryCache();

        services.AddHttpClient<ISrgSsrApi, SrgSsrApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://il.srgssr.ch/integrationlayer/2.0/");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });

        services.AddScoped<IRadioService, RadioService>();

        return services;
    }
}
