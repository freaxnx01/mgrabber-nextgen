using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MusicGrabber.Modules.Identity.Application.Ports.Driven;
using MusicGrabber.Modules.Identity.Application.Ports.Driving;
using MusicGrabber.Modules.Identity.Application.UseCases.GetUserProfile;
using MusicGrabber.Modules.Identity.Application.UseCases.ManageWhitelist;
using MusicGrabber.Modules.Identity.Application.UseCases.UpdateSettings;
using MusicGrabber.Modules.Identity.Domain;
using MusicGrabber.Modules.Identity.Infrastructure.Adapters;
using MusicGrabber.Modules.Identity.Infrastructure.Adapters.Persistence;
using MusicGrabber.Shared.Contracts;

namespace MusicGrabber.Modules.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, string connectionString)
    {
        // EF Core — Identity DbContext
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseSqlite(connectionString));

        // ASP.NET Core Identity — AddIdentity (not AddIdentityCore) registers
        // cookie authentication schemes needed for external OAuth providers
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddDefaultTokenProviders();

        // Configure cookie paths — default is /Account/Login which doesn't exist
        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/login";
            options.LogoutPath = "/logout";
            options.AccessDeniedPath = "/access-denied";
        });

        // Repositories (driven ports)
        services.AddScoped<IWhitelistRepository, WhitelistRepository>();
        services.AddScoped<IUserSettingsRepository, UserSettingsRepository>();

        // Use case handlers
        services.AddScoped<GetUserProfileHandler>();
        services.AddScoped<ManageWhitelistHandler>();
        services.AddScoped<UpdateSettingsHandler>();

        // Driving port adapters
        services.AddScoped<IWhitelistService, WhitelistServiceAdapter>();
        services.AddScoped<IUserSettingsService, UserSettingsServiceAdapter>();

        // Cross-module facade
        services.AddScoped<IUserFacade, UserFacadeAdapter>();

        // Claims transformation — adds role claims from whitelist on every auth request
        services.AddScoped<Microsoft.AspNetCore.Authentication.IClaimsTransformation, Adapters.WhitelistClaimsTransformation>();

        return services;
    }
}
