using MusicGrabber.Modules.Radio.Application.Ports.Driven;
using MusicGrabber.Modules.Radio.Application.UseCases.GetNowPlaying;
using MusicGrabber.Modules.Radio.Domain;

namespace MusicGrabber.Modules.Radio.UnitTests.UseCases;

public class GetNowPlayingHandlerTests
{
    private readonly ISrgSsrApi _api = Substitute.For<ISrgSsrApi>();
    private readonly GetNowPlayingHandler _sut;

    public GetNowPlayingHandlerTests()
    {
        _sut = new GetNowPlayingHandler(_api);
    }

    [Fact]
    public async Task GetNowPlayingAsync_DelegatesToApi()
    {
        var song = new RadioSong("Artist A", "Song Title", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(3));
        _api.GetNowPlayingAsync("srf-1", Arg.Any<CancellationToken>()).Returns(song);

        var result = await _sut.GetNowPlayingAsync("srf-1");

        result.Should().Be(song);
    }

    [Fact]
    public async Task GetNowPlayingAsync_WhenApiReturnsNull_ReturnsNull()
    {
        _api.GetNowPlayingAsync("srf-3", Arg.Any<CancellationToken>()).Returns((RadioSong?)null);

        var result = await _sut.GetNowPlayingAsync("srf-3");

        result.Should().BeNull();
    }
}
