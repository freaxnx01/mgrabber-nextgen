using MusicGrabber.Modules.Discovery.Application.Ports.Driven;
using MusicGrabber.Modules.Discovery.Application.UseCases.SearchArtists;
using MusicGrabber.Modules.Discovery.Domain;

namespace MusicGrabber.Modules.Discovery.UnitTests.UseCases;

public class SearchArtistsHandlerTests
{
    private readonly IMusicBrainzApi _api = Substitute.For<IMusicBrainzApi>();
    private readonly SearchArtistsHandler _sut;

    public SearchArtistsHandlerTests()
    {
        _sut = new SearchArtistsHandler(_api);
    }

    [Fact]
    public async Task SearchArtistsAsync_DelegatesToApi()
    {
        var expected = new List<ArtistResult>
        {
            new("1", "Radiohead", null, "GB", "Group", null, 100)
        };
        _api.SearchArtistsAsync("radiohead", 10, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.SearchArtistsAsync("radiohead");

        result.Should().BeEquivalentTo(expected);
    }
}
