using MusicGrabber.Modules.Radio.Domain;

namespace MusicGrabber.Frontend.Services;

public interface IRadioFrontendService
{
    Task<List<RadioStation>> GetStationsAsync(CancellationToken ct = default);
    Task<RadioSong?> GetNowPlayingAsync(string stationId, CancellationToken ct = default);
    Task<List<RadioSong>> GetPlaylistAsync(string stationId, int limit = 20, CancellationToken ct = default);
}
