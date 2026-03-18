using System.Net;
using System.Text.Json;
using DownloadApi.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace DownloadApi.UnitTests;

public class SrgSsrRadioServiceTests
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SrgSsrRadioService> _logger;
    private readonly IMemoryCache _cache;
    private readonly SrgSsrRadioService _service;
    private readonly HttpMessageHandlerMock _handlerMock;

    public SrgSsrRadioServiceTests()
    {
        _handlerMock = new HttpMessageHandlerMock();
        _httpClient = new HttpClient(_handlerMock);
        
        _configuration = Substitute.For<IConfiguration>();
        _configuration["Radio:SrgSsr:BaseUrl"].Returns("https://test.radio.com/api");
        _configuration["Radio:SrgSsr:UserAgent"].Returns("TestAgent/1.0");
        
        _logger = Substitute.For<ILogger<SrgSsrRadioService>>();
        _cache = Substitute.For<IMemoryCache>();
        
        _service = new SrgSsrRadioService(_httpClient, _configuration, _logger, _cache);
    }

    [Fact]
    public async Task GetStationsAsync_WithCachedData_ReturnsCachedStations()
    {
        // Arrange
        var cachedStations = new List<RadioStation>
        {
            new() { Id = "cached1", Name = "Cached Station 1", Provider = "srgssr" }
        };

        object? cacheValue = cachedStations;
        _cache.TryGetValue("radio_stations", out cacheValue).Returns(true);

        // Act
        var result = await _service.GetStationsAsync();

        // Assert
        result.Should().BeEquivalentTo(cachedStations);
        _handlerMock.RequestCount.Should().Be(0);
    }

    [Fact]
    public async Task GetStationsAsync_ApiSuccess_ReturnsStations()
    {
        // Arrange
        var mockResponse = new SrgChannelListResponse
        {
            Channels = new List<SrgChannel>
            {
                new() { Id = "69e8ac16-4327-4af4-b873-fd5cd6e895a7", Name = "Radio SRF 1" },
                new() { Id = "test-id-2", Name = "Radio SRF 2" }
            }
        };

        _handlerMock.SetResponse(JsonSerializer.Serialize(mockResponse), HttpStatusCode.OK);

        object? cacheValue = null;
        _cache.TryGetValue(Arg.Any<object>(), out cacheValue).Returns(false);

        // Act
        var result = await _service.GetStationsAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].Id.Should().Be("srf1"); // Mapped from known ID
        result[0].Name.Should().Be("Radio SRF 1");
        result[1].Id.Should().Be("test-id-2"); // Unknown ID, lowercased
    }

    [Fact]
    public async Task GetStationsAsync_ApiFailure_ReturnsDefaultStations()
    {
        // Arrange
        _handlerMock.SetResponse("error", HttpStatusCode.ServiceUnavailable);

        object? cacheValue = null;
        _cache.TryGetValue(Arg.Any<object>(), out cacheValue).Returns(false);

        // Act
        var result = await _service.GetStationsAsync();

        // Assert
        result.Should().HaveCount(6); // Default fallback stations
        result.Select(s => s.Id).Should().Contain("srf1", "srf2", "srf3", "srf4", "srfvirus", "srfmusikwelle");
    }

    [Fact]
    public async Task GetPlaylistAsync_WithCachedData_ReturnsCachedPlaylist()
    {
        // Arrange
        var stationId = "srf1";
        var cachedPlaylist = new List<RadioSong>
        {
            new() { Artist = "Test Artist", Title = "Test Song", Station = stationId }
        };

        object? cacheValue = cachedPlaylist;
        _cache.TryGetValue($"radio_playlist_{stationId}", out cacheValue).Returns(true);

        var stations = new List<RadioStation>
        {
            new() { Id = "srf1", Name = "Radio SRF 1", ChannelId = "69e8ac16-4327-4af4-b873-fd5cd6e895a7" }
        };
        _cache.TryGetValue("radio_stations", out cacheValue).Returns(true);

        // Act
        var result = await _service.GetPlaylistAsync(stationId);

        // Assert
        result.Should().BeEquivalentTo(cachedPlaylist);
    }

    [Fact]
    public async Task GetPlaylistAsync_ApiSuccess_ReturnsSongs()
    {
        // Arrange
        var stationId = "srf1";
        var mockResponse = new SrgSongListResponse
        {
            SongList = new List<SrgSong>
            {
                new()
                {
                    Title = "CREEP",
                    Artist = new SrgArtist { Name = "RADIOHEAD" },
                    Date = DateTime.UtcNow.ToString("O"),
                    Duration = 238000,
                    IsPlayingNow = true
                },
                new()
                {
                    Title = "KARMA POLICE",
                    Artist = new SrgArtist { Name = "RADIOHEAD" },
                    Date = DateTime.UtcNow.AddMinutes(-4).ToString("O"),
                    Duration = 264000,
                    IsPlayingNow = false
                }
            }
        };

        _handlerMock.SetResponse(JsonSerializer.Serialize(mockResponse), HttpStatusCode.OK);

        object? cacheValue = null;
        _cache.TryGetValue(Arg.Any<object>(), out cacheValue).Returns(false);

        var stations = new List<RadioStation>
        {
            new() { Id = "srf1", Name = "Radio SRF 1", ChannelId = "69e8ac16-4327-4af4-b873-fd5cd6e895a7" }
        };
        _cache.TryGetValue("radio_stations", out cacheValue).Returns(x => { x[1] = stations; return true; });

        // Act
        var result = await _service.GetPlaylistAsync(stationId);

        // Assert
        result.Should().HaveCount(2);
        result[0].Title.Should().Be("Creep"); // Normalized from UPPERCASE
        result[0].Artist.Should().Be("Radiohead");
        result[0].IsPlayingNow.Should().BeTrue();
    }

    [Fact]
    public async Task GetPlaylistAsync_InvalidStation_ThrowsArgumentException()
    {
        // Arrange
        object? cacheValue = null;
        _cache.TryGetValue(Arg.Any<object>(), out cacheValue).Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetPlaylistAsync("unknown-station"));
    }

    [Fact]
    public async Task GetNowPlayingAsync_ReturnsCurrentSong()
    {
        // Arrange
        var stationId = "srf1";
        var mockPlaylist = new List<RadioSong>
        {
            new() { Artist = "Radiohead", Title = "Creep", IsPlayingNow = true, Station = stationId },
            new() { Artist = "Radiohead", Title = "Karma Police", IsPlayingNow = false, Station = stationId }
        };

        object? cacheValue = mockPlaylist;
        _cache.TryGetValue($"radio_playlist_{stationId}", out cacheValue).Returns(true);

        var stations = new List<RadioStation>
        {
            new() { Id = "srf1", Name = "Radio SRF 1", ChannelId = "69e8ac16-4327-4af4-b873-fd5cd6e895a7" }
        };
        _cache.TryGetValue("radio_stations", out cacheValue).Returns(x => { x[1] = stations; return true; });

        // Act
        var result = await _service.GetNowPlayingAsync(stationId);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Creep");
        result.IsPlayingNow.Should().BeTrue();
    }

    [Fact]
    public async Task ResolveStationAsync_ById_ReturnsStation()
    {
        // Arrange
        var stations = new List<RadioStation>
        {
            new() { Id = "srf3", Name = "Radio SRF 3" },
            new() { Id = "srf1", Name = "Radio SRF 1" }
        };

        object? cacheValue = stations;
        _cache.TryGetValue("radio_stations", out cacheValue).Returns(true);

        // Act
        var result = await _service.ResolveStationAsync("srf1");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("srf1");
    }

    [Fact]
    public async Task ResolveStationAsync_ByName_ReturnsStation()
    {
        // Arrange
        var stations = new List<RadioStation>
        {
            new() { Id = "srf3", Name = "Radio SRF 3" }
        };

        object? cacheValue = stations;
        _cache.TryGetValue("radio_stations", out cacheValue).Returns(true);

        // Act
        var result = await _service.ResolveStationAsync("RadioSRF3");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("srf3");
    }

    [Theory]
    [InlineData("CREEP", "Creep")]
    [InlineData("KARMA POLICE", "Karma Police")]
    [InlineData("SONG NAME", "Song Name")]
    public void NormalizeTitle_VariousInputs_ReturnsTitleCase(string input, string expected)
    {
        // Act - using reflection to test private method
        var method = typeof(SrgSsrRadioService).GetMethod("NormalizeTitle", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = method!.Invoke(_service, new object?[] { input });

        // Assert
        result.Should().Be(expected);
    }

    // Response DTOs for mocking
    private class SrgChannelListResponse
    {
        public List<SrgChannel>? Channels { get; set; }
    }

    private class SrgChannel
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
    }

    private class SrgSongListResponse
    {
        public string? Next { get; set; }
        public List<SrgSong>? SongList { get; set; }
    }

    private class SrgSong
    {
        public bool IsPlayingNow { get; set; }
        public string? Date { get; set; }
        public int Duration { get; set; }
        public string? Title { get; set; }
        public SrgArtist? Artist { get; set; }
    }

    private class SrgArtist
    {
        public string? Name { get; set; }
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
