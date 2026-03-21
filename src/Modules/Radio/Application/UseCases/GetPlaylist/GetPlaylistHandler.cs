using MusicGrabber.Modules.Radio.Application.Ports.Driven;
using MusicGrabber.Modules.Radio.Domain;

namespace MusicGrabber.Modules.Radio.Application.UseCases.GetPlaylist;

public sealed class GetPlaylistHandler(ISrgSsrApi api)
{
    public Task<List<RadioSong>> GetPlaylistAsync(string stationId, int limit = 10, CancellationToken ct = default)
        => api.GetPlaylistAsync(stationId, limit, ct);
}
