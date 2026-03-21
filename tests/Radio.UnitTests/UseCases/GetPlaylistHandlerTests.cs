using MusicGrabber.Modules.Radio.Application.Ports.Driven;
using MusicGrabber.Modules.Radio.Application.UseCases.GetPlaylist;
using MusicGrabber.Modules.Radio.Domain;

namespace MusicGrabber.Modules.Radio.UnitTests.UseCases;

public class GetPlaylistHandlerTests
{
    private readonly ISrgSsrApi _api = Substitute.For<ISrgSsrApi>();
    private readonly GetPlaylistHandler _sut;

    public GetPlaylistHandlerTests()
    {
        _sut = new GetPlaylistHandler(_api);
    }

    [Fact]
    public async Task GetPlaylistAsync_DelegatesToApi()
    {
        var songs = new List<RadioSong>
        {
            new("Artist A", "Song 1", DateTimeOffset.UtcNow.AddMinutes(-10), DateTimeOffset.UtcNow.AddMinutes(-7)),
            new("Artist B", "Song 2", DateTimeOffset.UtcNow.AddMinutes(-7), DateTimeOffset.UtcNow.AddMinutes(-4))
        };
        _api.GetPlaylistAsync("srf-1", 10, Arg.Any<CancellationToken>()).Returns(songs);

        var result = await _sut.GetPlaylistAsync("srf-1", 10);

        result.Should().BeEquivalentTo(songs);
    }

    [Fact]
    public async Task GetPlaylistAsync_UnknownStationId_ReturnsEmptyList()
    {
        _api.GetPlaylistAsync("unknown-station", Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);

        var result = await _sut.GetPlaylistAsync("unknown-station", 10);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPlaylistAsync_WithCustomLimit_PassesLimitToApi()
    {
        _api.GetPlaylistAsync("srf-3", 5, Arg.Any<CancellationToken>()).Returns([]);

        await _sut.GetPlaylistAsync("srf-3", 5);

        await _api.Received(1).GetPlaylistAsync("srf-3", 5, Arg.Any<CancellationToken>());
    }
}
