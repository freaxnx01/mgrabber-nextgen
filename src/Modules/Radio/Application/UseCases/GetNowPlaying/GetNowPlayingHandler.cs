using MusicGrabber.Modules.Radio.Application.Ports.Driven;
using MusicGrabber.Modules.Radio.Domain;

namespace MusicGrabber.Modules.Radio.Application.UseCases.GetNowPlaying;

public sealed class GetNowPlayingHandler(ISrgSsrApi api)
{
    public Task<RadioSong?> GetNowPlayingAsync(string stationId, CancellationToken ct = default)
        => api.GetNowPlayingAsync(stationId, ct);
}
