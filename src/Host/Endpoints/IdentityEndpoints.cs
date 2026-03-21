using MusicGrabber.Modules.Identity.Application.Ports.Driving;
using MusicGrabber.Shared.Contracts;

namespace MusicGrabber.Host.Endpoints;

public static class IdentityEndpoints
{
    public static void MapIdentityEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/users").RequireAuthorization();

        group.MapGet("/{userId}/profile", async (string userId, IUserFacade facade, CancellationToken ct) =>
        {
            var profile = await facade.GetProfileAsync(userId, ct);
            return profile is not null ? Results.Ok(profile) : Results.NotFound();
        });

        group.MapGet("/{userId}/settings", async (string userId, IUserSettingsService service, CancellationToken ct) =>
            Results.Ok(await service.GetOrCreateAsync(userId, ct)));

        group.MapPut("/{userId}/settings", async (string userId, UpdateSettingsRequest request,
            IUserSettingsService service, CancellationToken ct) =>
        {
            var settings = await service.UpdateAsync(userId, request.DefaultFormat,
                request.EnableNormalization, request.NormalizationLufs, request.EmailNotifications, ct);
            return Results.Ok(settings);
        });
    }
}

public sealed record UpdateSettingsRequest(
    string DefaultFormat, bool EnableNormalization, int NormalizationLufs, bool EmailNotifications);
