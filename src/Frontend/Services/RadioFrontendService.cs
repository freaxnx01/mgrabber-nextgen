using MusicGrabber.Modules.Radio.Application.Ports.Driving;
using MusicGrabber.Modules.Radio.Domain;

namespace MusicGrabber.Frontend.Services;

public sealed class RadioFrontendService(IRadioService radioService) : IRadioFrontendService
{
    public Task<List<RadioStation>> GetStationsAsync(CancellationToken ct)
        => radioService.GetStationsAsync(ct);

    public Task<RadioSong?> GetNowPlayingAsync(string stationId, CancellationToken ct)
        => radioService.GetNowPlayingAsync(stationId, ct);

    public Task<List<RadioSong>> GetPlaylistAsync(string stationId, int limit, CancellationToken ct)
        => radioService.GetPlaylistAsync(stationId, limit, ct);
}
