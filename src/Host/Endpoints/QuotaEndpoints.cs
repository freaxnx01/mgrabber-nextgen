using MusicGrabber.Modules.Quota.Application.Ports.Driving;

namespace MusicGrabber.Host.Endpoints;

public static class QuotaEndpoints
{
    public static void MapQuotaEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/quotas").RequireAuthorization();

        group.MapGet("/users/{userId}", async (string userId, IQuotaService service, CancellationToken ct) =>
            Results.Ok(await service.GetQuotaAsync(userId, ct)));

        group.MapGet("/users/{userId}/check", async (string userId, long bytes, IQuotaService service, CancellationToken ct) =>
            Results.Ok(new { Allowed = await service.CheckAsync(userId, bytes, ct) }));
    }
}
