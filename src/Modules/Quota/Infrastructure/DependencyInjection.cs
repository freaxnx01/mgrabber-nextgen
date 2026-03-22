using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MusicGrabber.Modules.Quota.Application.Ports.Driven;
using MusicGrabber.Modules.Quota.Application.Ports.Driving;
using MusicGrabber.Modules.Quota.Application.UseCases.CheckQuota;
using MusicGrabber.Modules.Quota.Application.UseCases.SendThresholdNotification;
using MusicGrabber.Modules.Quota.Infrastructure.Adapters;
using MusicGrabber.Modules.Quota.Infrastructure.Adapters.Persistence;
using MusicGrabber.Shared.Contracts;

namespace MusicGrabber.Modules.Quota.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddQuotaModule(this IServiceCollection services, string connectionString)
    {
        // EF Core — Quota DbContext
        services.AddDbContext<QuotaDbContext>(options =>
            options.UseSqlite(connectionString));

        // Repositories (driven ports)
        services.AddScoped<IQuotaRepository, QuotaRepository>();

        // Email adapter (driven port)
        services.AddScoped<IEmailService, SmtpEmailAdapter>();

        // Use case handlers
        services.AddScoped<CheckQuotaHandler>();
        services.AddScoped<SendThresholdNotificationHandler>();

        // Driving port adapter
        services.AddScoped<IQuotaService, QuotaServiceAdapter>();

        // Cross-module facade
        services.AddScoped<IQuotaFacade, QuotaFacadeAdapter>();

        return services;
    }
}
