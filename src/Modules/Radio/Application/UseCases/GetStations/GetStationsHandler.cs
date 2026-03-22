using MusicGrabber.Modules.Radio.Application.Ports.Driven;
using MusicGrabber.Modules.Radio.Domain;

namespace MusicGrabber.Modules.Radio.Application.UseCases.GetStations;

public sealed class GetStationsHandler(ISrgSsrApi api)
{
    public Task<List<RadioStation>> GetStationsAsync(CancellationToken ct = default)
        => api.GetStationsAsync(ct);
}
