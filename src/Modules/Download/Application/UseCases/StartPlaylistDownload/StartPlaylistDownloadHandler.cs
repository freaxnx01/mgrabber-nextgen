using MusicGrabber.Modules.Download.Application.Ports.Driven;
using MusicGrabber.Modules.Download.Domain;
using MusicGrabber.Shared.Contracts;

namespace MusicGrabber.Modules.Download.Application.UseCases.StartPlaylistDownload;

public sealed class StartPlaylistDownloadHandler(
    IDownloadJobRepository repo,
    IQuotaFacade quotaFacade)
{
    public async Task<List<Guid>> StartPlaylistAsync(
        string playlistId, string userId, List<string> videoUrls,
        string format, bool normalize, CancellationToken ct = default)
    {
        var quotaOk = await quotaFacade.CheckAsync(userId, 0, ct);
        if (!quotaOk)
            throw new InvalidOperationException("Storage quota exceeded.");

        var audioFormat = Enum.Parse<AudioFormat>(format, ignoreCase: true);
        var jobIds = new List<Guid>();

        foreach (var url in videoUrls)
        {
            var job = DownloadJob.Create(url, userId, audioFormat,
                normalizeAudio: normalize, playlistId: playlistId);
            await repo.AddAsync(job, ct);
            jobIds.Add(job.Id);
        }

        return jobIds;
    }
}
