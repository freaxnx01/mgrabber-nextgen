using MusicGrabber.Modules.Radio.Application.Ports.Driven;
using MusicGrabber.Modules.Radio.Application.UseCases.GetStations;
using MusicGrabber.Modules.Radio.Domain;

namespace MusicGrabber.Modules.Radio.UnitTests.UseCases;

public class GetStationsHandlerTests
{
    private readonly ISrgSsrApi _api = Substitute.For<ISrgSsrApi>();
    private readonly GetStationsHandler _sut;

    public GetStationsHandlerTests()
    {
        _sut = new GetStationsHandler(_api);
    }

    [Fact]
    public async Task GetStationsAsync_DelegatesToApi()
    {
        var expected = new List<RadioStation>
        {
            new("srf-1", "SRF 1"),
            new("srf-3", "SRF 3"),
            new("srf-virus", "SRF Virus")
        };
        _api.GetStationsAsync(Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.GetStationsAsync();

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetStationsAsync_WhenApiReturnsEmpty_ReturnsEmptyList()
    {
        _api.GetStationsAsync(Arg.Any<CancellationToken>()).Returns([]);

        var result = await _sut.GetStationsAsync();

        result.Should().BeEmpty();
    }
}
