using MusicGrabber.Modules.Download.Application.Ports.Driven;
using MusicGrabber.Modules.Download.Domain;
using MusicGrabber.Shared.Contracts;
using MusicGrabber.Shared.DTOs;

namespace MusicGrabber.Modules.Download.Application.UseCases.StartDownload;

public sealed class StartDownloadHandler(
    IDownloadJobRepository repo,
    IQuotaFacade quotaFacade)
{
    private const int MaxPerUserConcurrent = 3;
    private const int MaxGlobalConcurrent = 9;

    public async Task<Guid> StartAsync(StartDownloadRequest request, CancellationToken ct = default)
    {
        var quotaOk = await quotaFacade.CheckAsync(request.UserId, 0, ct);
        if (!quotaOk)
            throw new InvalidOperationException("Storage quota exceeded.");

        var activeCount = await repo.GetActiveCountByUserIdAsync(request.UserId, ct);
        if (activeCount >= MaxPerUserConcurrent)
            throw new InvalidOperationException($"Maximum {MaxPerUserConcurrent} concurrent downloads reached.");

        var globalActive = await repo.GetActiveCountAsync(ct);
        if (globalActive >= MaxGlobalConcurrent)
            throw new InvalidOperationException("Maximum global concurrent downloads reached.");

        var format = Enum.Parse<AudioFormat>(request.Format, ignoreCase: true);
        var job = DownloadJob.Create(
            url: request.Url,
            userId: request.UserId,
            format: format,
            title: request.Title,
            author: request.Author,
            normalizeAudio: request.NormalizeAudio);

        await repo.AddAsync(job, ct);
        return job.Id;
    }
}
