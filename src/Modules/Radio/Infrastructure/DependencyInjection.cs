using Microsoft.Extensions.DependencyInjection;

namespace MusicGrabber.Modules.Radio.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddRadioModule(this IServiceCollection services)
    {
        return services;
    }
}
