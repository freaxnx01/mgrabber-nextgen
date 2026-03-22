using MusicGrabber.Modules.Download.Application.UseCases.GetDownloadStats;
using MusicGrabber.Modules.Identity.Application.Ports.Driving;

namespace MusicGrabber.Host.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/admin").RequireAuthorization(policy =>
            policy.RequireRole("Admin"));

        group.MapGet("/stats", async (GetDownloadStatsHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.GetGlobalStatsAsync(ct)));

        // Errata #19: list all user stats
        group.MapGet("/stats/users", async (GetDownloadStatsHandler handler, CancellationToken ct) =>
        {
            var global = await handler.GetGlobalStatsAsync(ct);
            return Results.Ok(global.Users);
        });

        // Errata #19: single user detail stats
        group.MapGet("/stats/users/{userId}", async (string userId, GetDownloadStatsHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.GetUserStatsAsync(userId, ct)));

        group.MapGet("/whitelist", async (IWhitelistService service, CancellationToken ct) =>
            Results.Ok(await service.GetAllAsync(ct)));

        group.MapPost("/whitelist", async (AddWhitelistRequest request, IWhitelistService service, CancellationToken ct) =>
        {
            var entry = await service.AddAsync(request.UserId, request.Role, "admin", ct);
            return Results.Created($"/api/v1/admin/whitelist/{entry.Id}", entry);
        });

        group.MapPut("/whitelist/{id:guid}", async (Guid id, IWhitelistService service, CancellationToken ct) =>
        {
            await service.ToggleAsync(id, ct);
            return Results.NoContent();
        });

        group.MapDelete("/whitelist/{id:guid}", async (Guid id, IWhitelistService service, CancellationToken ct) =>
        {
            await service.RemoveAsync(id, ct);
            return Results.NoContent();
        });
    }
}

public sealed record AddWhitelistRequest(string UserId, string Role);
