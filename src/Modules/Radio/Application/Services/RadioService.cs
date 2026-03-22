using MusicGrabber.Modules.Radio.Application.Ports.Driven;
using MusicGrabber.Modules.Radio.Application.Ports.Driving;
using MusicGrabber.Modules.Radio.Domain;

namespace MusicGrabber.Modules.Radio.Application.Services;

public sealed class RadioService(ISrgSsrApi api) : IRadioService
{
    public Task<List<RadioStation>> GetStationsAsync(CancellationToken ct = default)
        => api.GetStationsAsync(ct);

    public Task<RadioSong?> GetNowPlayingAsync(string stationId, CancellationToken ct = default)
        => api.GetNowPlayingAsync(stationId, ct);

    public Task<List<RadioSong>> GetPlaylistAsync(string stationId, int limit = 10, CancellationToken ct = default)
        => api.GetPlaylistAsync(stationId, limit, ct);
}
