using System.Net;
using System.Text.Json;
using DownloadApi.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace DownloadApi.UnitTests;

public class YouTubeSearchServiceTests
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<YouTubeSearchService> _logger;
    private readonly IMemoryCache _cache;
    private readonly YouTubeSearchService _service;
    private readonly HttpMessageHandlerMock _handlerMock;

    public YouTubeSearchServiceTests()
    {
        _handlerMock = new HttpMessageHandlerMock();
        _httpClient = new HttpClient(_handlerMock);
        
        _configuration = Substitute.For<IConfiguration>();
        _configuration["YouTube:ApiKey"].Returns("test-api-key");
        
        _logger = Substitute.For<ILogger<YouTubeSearchService>>();
        _cache = Substitute.For<IMemoryCache>();
        
        _service = new YouTubeSearchService(_httpClient, _configuration, _logger, _cache);
    }

    [Fact]
    public async Task SearchAsync_WithValidQuery_ReturnsResults()
    {
        // Arrange
        var query = "test song";
        var mockResponse = new YouTubeApiResponse
        {
            PageInfo = new PageInfo { TotalResults = 2, ResultsPerPage = 10 },
            Items = new List<SearchItem>
            {
                new()
                {
                    Id = new ItemId { VideoId = "abc123" },
                    Snippet = new Snippet
                    {
                        Title = "Test Song 1",
                        ChannelTitle = "Artist 1",
                        Thumbnails = new Thumbnails { Medium = new Thumbnail { Url = "http://thumb1.jpg" } }
                    }
                },
                new()
                {
                    Id = new ItemId { VideoId = "def456" },
                    Snippet = new Snippet
                    {
                        Title = "Test Song 2",
                        ChannelTitle = "Artist 2",
                        Thumbnails = new Thumbnails { Medium = new Thumbnail { Url = "http://thumb2.jpg" } }
                    }
                }
            }
        };

        _handlerMock.SetResponse(JsonSerializer.Serialize(mockResponse), HttpStatusCode.OK);

        object? cacheValue = null;
        _cache.TryGetValue(Arg.Any<object>(), out cacheValue).Returns(false);

        // Act
        var result = await _service.SearchAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Query.Should().Be(query);
        result.Results.Should().HaveCount(2);
        result.Results[0].VideoId.Should().Be("abc123");
        result.Results[0].Title.Should().Be("Test Song 1");
        result.Results[0].Author.Should().Be("Artist 1");
    }

    [Fact]
    public async Task SearchAsync_WithEmptyQuery_ReturnsEmptyResults()
    {
        // Arrange
        var mockResponse = new YouTubeApiResponse
        {
            PageInfo = new PageInfo { TotalResults = 0, ResultsPerPage = 10 },
            Items = new List<SearchItem>()
        };

        _handlerMock.SetResponse(JsonSerializer.Serialize(mockResponse), HttpStatusCode.OK);

        object? cacheValue = null;
        _cache.TryGetValue(Arg.Any<object>(), out cacheValue).Returns(false);

        // Act
        var result = await _service.SearchAsync("");

        // Assert
        result.Should().NotBeNull();
        result.Results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_WithCachedResult_ReturnsCachedData()
    {
        // Arrange
        var query = "cached query";
        var cachedResult = new YouTubeSearchResult
        {
            Query = query,
            Results = new List<YouTubeVideo>
            {
                new() { VideoId = "cached123", Title = "Cached Song", Author = "Cached Artist" }
            }
        };

        object? cacheValue = cachedResult;
        _cache.TryGetValue(Arg.Any<object>(), out cacheValue).Returns(true);

        // Act
        var result = await _service.SearchAsync(query);

        // Assert
        result.Should().BeEquivalentTo(cachedResult);
        _handlerMock.RequestCount.Should().Be(0); // No HTTP call made
    }

    [Fact]
    public async Task SearchAsync_WithInvalidApiKey_ThrowsInvalidOperationException()
    {
        // Arrange
        _configuration["YouTube:ApiKey"].Returns((string?)null);
        var serviceWithNoKey = new YouTubeSearchService(_httpClient, _configuration, _logger, _cache);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => serviceWithNoKey.SearchAsync("test"));
    }

    [Fact]
    public async Task SearchAsync_WithForbiddenResponse_ThrowsInvalidOperationException()
    {
        // Arrange
        _handlerMock.SetResponse("{}", HttpStatusCode.Forbidden);

        object? cacheValue = null;
        _cache.TryGetValue(Arg.Any<object>(), out cacheValue).Returns(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SearchAsync("test"));
        exception.Message.Should().Contain("YouTube API error");
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
