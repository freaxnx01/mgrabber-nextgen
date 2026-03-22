using MusicGrabber.Modules.Radio.Application.Ports.Driven;
using MusicGrabber.Modules.Radio.Application.Services;
using MusicGrabber.Modules.Radio.Domain;

namespace MusicGrabber.Modules.Radio.UnitTests.Services;

public class RadioServiceTests
{
    private readonly ISrgSsrApi _api = Substitute.For<ISrgSsrApi>();
    private readonly RadioService _sut;

    public RadioServiceTests()
    {
        _sut = new RadioService(_api);
    }

    [Fact]
    public async Task GetStationsAsync_DelegatesToApi()
    {
        var stations = new List<RadioStation>
        {
            new("srf-1", "SRF 1"),
            new("srf-3", "SRF 3")
        };
        _api.GetStationsAsync(Arg.Any<CancellationToken>()).Returns(stations);

        var result = await _sut.GetStationsAsync();

        result.Should().BeEquivalentTo(stations);
    }

    [Fact]
    public async Task GetNowPlayingAsync_DelegatesToApi()
    {
        var song = new RadioSong("DJ Bobo", "Chihuahua", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(4));
        _api.GetNowPlayingAsync("srf-virus", Arg.Any<CancellationToken>()).Returns(song);

        var result = await _sut.GetNowPlayingAsync("srf-virus");

        result.Should().Be(song);
    }

    [Fact]
    public async Task GetNowPlayingAsync_WhenApiReturnsNull_ReturnsNull()
    {
        _api.GetNowPlayingAsync("srf-1", Arg.Any<CancellationToken>()).Returns((RadioSong?)null);

        var result = await _sut.GetNowPlayingAsync("srf-1");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPlaylistAsync_DelegatesToApi()
    {
        var songs = new List<RadioSong>
        {
            new("Artist X", "Track 1", DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddMinutes(-2))
        };
        _api.GetPlaylistAsync("srf-3", 10, Arg.Any<CancellationToken>()).Returns(songs);

        var result = await _sut.GetPlaylistAsync("srf-3", 10);

        result.Should().BeEquivalentTo(songs);
    }

    [Fact]
    public async Task GetPlaylistAsync_UnknownStation_ReturnsEmptyList()
    {
        _api.GetPlaylistAsync("not-a-station", Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);

        var result = await _sut.GetPlaylistAsync("not-a-station", 10);

        result.Should().BeEmpty();
    }
}
