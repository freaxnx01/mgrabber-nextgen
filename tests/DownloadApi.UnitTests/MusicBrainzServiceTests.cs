using System.Net;
using System.Text.Json;
using DownloadApi.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace DownloadApi.UnitTests;

public class MusicBrainzServiceTests
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MusicBrainzService> _logger;
    private readonly IMemoryCache _cache;
    private readonly MusicBrainzService _service;
    private readonly HttpMessageHandlerMock _handlerMock;

    public MusicBrainzServiceTests()
    {
        _handlerMock = new HttpMessageHandlerMock();
        _httpClient = new HttpClient(_handlerMock);
        
        _logger = Substitute.For<ILogger<MusicBrainzService>>();
        _cache = Substitute.For<IMemoryCache>();
        
        _service = new MusicBrainzService(_httpClient, _logger, _cache);
    }

    [Fact]
    public async Task SearchArtistsAsync_WithValidQuery_ReturnsResults()
    {
        // Arrange
        var query = "radiohead";
        var mockResponse = new MusicBrainzSearchResponse
        {
            Count = 2,
            Artists = new List<MusicBrainzArtistResponse>
            {
                new()
                {
                    Id = "a74b1b7f-71a5-4011-9441-d0b5e4122711",
                    Name = "Radiohead",
                    SortName = "Radiohead",
                    Country = "GB",
                    Type = "Group",
                    Score = 100
                },
                new()
                {
                    Id = "test-id-2",
                    Name = "Radiohead Tribute Band",
                    SortName = "Radiohead Tribute Band",
                    Country = "US",
                    Type = "Group",
                    Score = 50
                }
            }
        };

        _handlerMock.SetResponse(JsonSerializer.Serialize(mockResponse), HttpStatusCode.OK);

        object? cacheValue = null;
        _cache.TryGetValue(Arg.Any<object>(), out cacheValue).Returns(false);

        // Act
        var result = await _service.SearchArtistsAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Query.Should().Be(query);
        result.Type.Should().Be("artist");
        result.Artists.Should().HaveCount(2);
        result.Artists[0].Name.Should().Be("Radiohead");
        result.Artists[0].Id.Should().Be("a74b1b7f-71a5-4011-9441-d0b5e4122711");
    }

    [Fact]
    public async Task SearchTracksAsync_WithValidQuery_ReturnsResults()
    {
        // Arrange
        var query = "creep radiohead";
        var mockResponse = new MusicBrainzRecordingResponse
        {
            Count = 1,
            Recordings = new List<MusicBrainzRecording>
            {
                new()
                {
                    Id = "track-id-1",
                    Title = "Creep",
                    Length = 238000,
                    Score = 100,
                    ArtistCredit = new List<ArtistCredit>
                    {
                        new() { Name = "Radiohead", Artist = new ArtistRef { Id = "artist-id-1" } }
                    }
                }
            }
        };

        _handlerMock.SetResponse(JsonSerializer.Serialize(mockResponse), HttpStatusCode.OK);

        object? cacheValue = null;
        _cache.TryGetValue(Arg.Any<object>(), out cacheValue).Returns(false);

        // Act
        var result = await _service.SearchTracksAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Query.Should().Be(query);
        result.Type.Should().Be("track");
        result.Tracks.Should().HaveCount(1);
        result.Tracks[0].Title.Should().Be("Creep");
    }

    [Fact]
    public async Task SearchReleasesAsync_WithValidQuery_ReturnsResults()
    {
        // Arrange
        var query = "ok computer";
        var mockResponse = new MusicBrainzReleaseResponse
        {
            Count = 1,
            Releases = new List<MusicBrainzRelease>
            {
                new()
                {
                    Id = "release-id-1",
                    Title = "OK Computer",
                    Date = "1997-05-28",
                    Country = "GB",
                    TrackCount = 12,
                    Score = 100,
                    ArtistCredit = new List<ArtistCredit>
                    {
                        new() { Name = "Radiohead", Artist = new ArtistRef { Id = "artist-id-1" } }
                    }
                }
            }
        };

        _handlerMock.SetResponse(JsonSerializer.Serialize(mockResponse), HttpStatusCode.OK);

        object? cacheValue = null;
        _cache.TryGetValue(Arg.Any<object>(), out cacheValue).Returns(false);

        // Act
        var result = await _service.SearchReleasesAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Query.Should().Be(query);
        result.Type.Should().Be("album");
        result.Releases.Should().HaveCount(1);
        result.Releases[0].Title.Should().Be("OK Computer");
    }

    [Fact]
    public async Task GetArtistDetailsAsync_WithValidId_ReturnsArtist()
    {
        // Arrange
        var artistId = "a74b1b7f-71a5-4011-9441-d0b5e4122711";
        var mockResponse = new MusicBrainzArtistResponse
        {
            Id = artistId,
            Name = "Radiohead",
            SortName = "Radiohead",
            Country = "GB",
            Type = "Group",
            Disambiguation = "English rock band"
        };

        _handlerMock.SetResponse(JsonSerializer.Serialize(mockResponse), HttpStatusCode.OK);

        object? cacheValue = null;
        _cache.TryGetValue(Arg.Any<object>(), out cacheValue).Returns(false);

        // Act
        var result = await _service.GetArtistDetailsAsync(artistId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(artistId);
        result.Name.Should().Be("Radiohead");
    }

    [Fact]
    public async Task SearchArtistsAsync_WithCachedResult_ReturnsCachedData()
    {
        // Arrange
        var query = "cached artist";
        var cachedResult = new MusicBrainzSearchResult
        {
            Query = query,
            Type = "artist",
            Artists = new List<MusicBrainzArtist>
            {
                new() { Id = "cached-id", Name = "Cached Artist" }
            }
        };

        object? cacheValue = cachedResult;
        _cache.TryGetValue(Arg.Any<object>(), out cacheValue).Returns(true);

        // Act
        var result = await _service.SearchArtistsAsync(query);

        // Assert
        result.Should().BeEquivalentTo(cachedResult);
        _handlerMock.RequestCount.Should().Be(0);
    }

    // Response DTOs for mocking
    private class MusicBrainzSearchResponse
    {
        public int Count { get; set; }
        public List<MusicBrainzArtistResponse>? Artists { get; set; }
    }

    private class MusicBrainzArtistResponse
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? SortName { get; set; }
        public string? Country { get; set; }
        public string? Type { get; set; }
        public string? Disambiguation { get; set; }
        public int? Score { get; set; }
    }

    private class MusicBrainzRecordingResponse
    {
        public int Count { get; set; }
        public List<MusicBrainzRecording>? Recordings { get; set; }
    }

    private class MusicBrainzRecording
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public long? Length { get; set; }
        public int? Score { get; set; }
        public List<ArtistCredit>? ArtistCredit { get; set; }
    }

    private class ArtistCredit
    {
        public string? Name { get; set; }
        public ArtistRef? Artist { get; set; }
    }

    private class ArtistRef
    {
        public string? Id { get; set; }
    }

    private class MusicBrainzReleaseResponse
    {
        public int Count { get; set; }
        public List<MusicBrainzRelease>? Releases { get; set; }
    }

    private class MusicBrainzRelease
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public string? Date { get; set; }
        public string? Country { get; set; }
        public int? TrackCount { get; set; }
        public int? Score { get; set; }
        public List<ArtistCredit>? ArtistCredit { get; set; }
    }

    private class HttpMessageHandlerMock : HttpMessageHandler
    {
        private string _responseContent = "{}";
        private HttpStatusCode _statusCode = HttpStatusCode.OK;
        public int RequestCount { get; private set; }

        public void SetResponse(string content, HttpStatusCode statusCode)
        {
            _responseContent = content;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseContent)
            });
        }
    }
}
