using MusicGrabber.Modules.Radio.Domain;

namespace MusicGrabber.Modules.Radio.Application.Ports.Driven;

public interface ISrgSsrApi
{
    Task<List<RadioStation>> GetStationsAsync(CancellationToken ct = default);
    Task<RadioSong?> GetNowPlayingAsync(string stationId, CancellationToken ct = default);
    Task<List<RadioSong>> GetPlaylistAsync(string stationId, int limit = 10, CancellationToken ct = default);
}
