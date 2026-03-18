using System.Net;
using System.Text.Json;
using DownloadApi.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace DownloadApi.UnitTests;

public class YouTubePlaylistServiceTests
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<YouTubePlaylistService> _logger;
    private readonly YouTubePlaylistService _service;
    private readonly HttpMessageHandlerMock _handlerMock;

    public YouTubePlaylistServiceTests()
    {
        _handlerMock = new HttpMessageHandlerMock();
        _httpClient = new HttpClient(_handlerMock);
        
        _configuration = Substitute.For<IConfiguration>();
        _configuration["YouTube:ApiKey"].Returns("test-api-key");
        
        _logger = Substitute.For<ILogger<YouTubePlaylistService>>();
        _service = new YouTubePlaylistService(_httpClient, _configuration, _logger);
    }

    [Theory]
    [InlineData("https://youtube.com/playlist?list=PL1234567890", "PL1234567890")]
    [InlineData("https://www.youtube.com/watch?v=abc&list=PLabcdefghij", "PLabcdefghij")]
    [InlineData("https://youtube.com/playlist?list=PLabc_def-123", "PLabc_def-123")]
    public void ExtractPlaylistId_ValidUrls_ReturnsId(string url, string expectedId)
    {
        // Act
        var result = YouTubePlaylistService.ExtractPlaylistId(url);

        // Assert
        result.Should().Be(expectedId);
    }

    [Theory]
    [InlineData("https://youtube.com/watch?v=abc")]
    [InlineData("invalid-url")]
    [InlineData("")]
    public void ExtractPlaylistId_InvalidUrls_ReturnsNull(string url)
    {
        // Act
        var result = YouTubePlaylistService.ExtractPlaylistId(url);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPlaylistInfoAsync_ValidUrl_ReturnsInfo()
    {
        // Arrange
        var mockResponse = new YouTubePlaylistResponse
        {
            Items = new List<PlaylistItem>
            {
                new()
                {
                    Id = "PL123",
                    Snippet = new PlaylistSnippet
                    {
                        Title = "My Test Playlist",
                        Description = "A test playlist",
                        ChannelTitle = "Test Channel",
                        PublishedAt = "2024-01-15T10:00:00Z",
                        Thumbnails = new ThumbnailSet
                        {
                            Medium = new ThumbnailInfo { Url = "https://thumb.jpg" }
                        }
                    },
                    ContentDetails = new PlaylistContentDetails { ItemCount = 25 }
                }
            }
        };

        _handlerMock.SetResponse(JsonSerializer.Serialize(mockResponse), HttpStatusCode.OK);

        // Act
        var result = await _service.GetPlaylistInfoAsync("https://youtube.com/playlist?list=PL123");

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("PL123");
        result.Title.Should().Be("My Test Playlist");
        result.Description.Should().Be("A test playlist");
        result.Author.Should().Be("Test Channel");
        result.VideoCount.Should().Be(25);
        result.ThumbnailUrl.Should().Be("https://thumb.jpg");
        result.PublishedAt.Should().Be("2024-01-15T10:00:00Z");
    }

    [Fact]
    public async Task GetPlaylistInfoAsync_InvalidUrl_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.GetPlaylistInfoAsync("https://youtube.com/watch?v=abc"));
    }

    [Fact]
    public async Task GetPlaylistInfoAsync_NotFound_ThrowsException()
    {
        // Arrange
        var mockResponse = new YouTubePlaylistResponse { Items = new List<PlaylistItem>() };
        _handlerMock.SetResponse(JsonSerializer.Serialize(mockResponse), HttpStatusCode.OK);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => 
            _service.GetPlaylistInfoAsync("https://youtube.com/playlist?list=PL123"));
    }

    [Fact]
    public async Task GetPlaylistVideosAsync_SinglePage_ReturnsVideos()
    {
        // Arrange
        var mockResponse = new YouTubePlaylistItemsResponse
        {
            Items = new List<PlaylistItemDetail>
            {
                new()
                {
                    Snippet = new PlaylistItemSnippet
                    {
                        Position = 0,
                        Title = "Video 1",
                        VideoOwnerChannelTitle = "Channel 1",
                        PublishedAt = "2024-01-01",
                        ResourceId = new ResourceId { VideoId = "vid1" },
                        Thumbnails = new ThumbnailSet { Medium = new ThumbnailInfo { Url = "https://thumb1.jpg" } }
                    }
                },
                new()
                {
                    Snippet = new PlaylistItemSnippet
                    {
                        Position = 1,
                        Title = "Video 2",
                        VideoOwnerChannelTitle = "Channel 2",
                        PublishedAt = "2024-01-02",
                        ResourceId = new ResourceId { VideoId = "vid2" },
                        Thumbnails = new ThumbnailSet { Medium = new ThumbnailInfo { Url = "https://thumb2.jpg" } }
                    }
                }
            },
            NextPageToken = null
        };

        _handlerMock.SetResponse(JsonSerializer.Serialize(mockResponse), HttpStatusCode.OK);

        // Act
        var result = await _service.GetPlaylistVideosAsync("PL123");

        // Assert
        result.Should().HaveCount(2);
        result[0].VideoId.Should().Be("vid1");
        result[0].Title.Should().Be("Video 1");
        result[0].Position.Should().Be(0);
        result[1].VideoId.Should().Be("vid2");
    }

    [Fact]
    public async Task GetPlaylistVideosAsync_MultiplePages_ReturnsAllVideos()
    {
        // Arrange - First page
        var page1Response = new YouTubePlaylistItemsResponse
        {
            Items = new List<PlaylistItemDetail>
            {
                new()
                {
                    Snippet = new PlaylistItemSnippet
                    {
                        Position = 0,
                        Title = "Video 1",
                        VideoOwnerChannelTitle = "Channel 1",
                        ResourceId = new ResourceId { VideoId = "vid1" }
                    }
                }
            },
            NextPageToken = "token2"
        };

        // Second page
        var page2Response = new YouTubePlaylistItemsResponse
        {
            Items = new List<PlaylistItemDetail>
            {
                new()
                {
                    Snippet = new PlaylistItemSnippet
                    {
                        Position = 1,
                        Title = "Video 2",
                        VideoOwnerChannelTitle = "Channel 2",
                        ResourceId = new ResourceId { VideoId = "vid2" }
                    }
                }
            },
            NextPageToken = null
        };

        _handlerMock.SetResponseSequence(new[]
        {
            (JsonSerializer.Serialize(page1Response), HttpStatusCode.OK),
            (JsonSerializer.Serialize(page2Response), HttpStatusCode.OK)
        });

        // Act
        var result = await _service.GetPlaylistVideosAsync("PL123");

        // Assert
        result.Should().HaveCount(2);
        result[0].VideoId.Should().Be("vid1");
        result[1].VideoId.Should().Be("vid2");
        _handlerMock.RequestCount.Should().Be(2);
    }

    [Fact]
    public void PlaylistInfo_HasCorrectDefaults()
    {
        // Arrange
        var info = new PlaylistInfo
        {
            Id = "PL123",
            Title = "Test",
            Description = "Desc",
            Author = "Author",
            ThumbnailUrl = "https://thumb.jpg",
            VideoCount = 10,
            PublishedAt = "2024-01-01"
        };

        // Assert
        info.Id.Should().Be("PL123");
        info.Title.Should().Be("Test");
        info.Description.Should().Be("Desc");
        info.Author.Should().Be("Author");
        info.ThumbnailUrl.Should().Be("https://thumb.jpg");
        info.VideoCount.Should().Be(10);
        info.PublishedAt.Should().Be("2024-01-01");
    }

    [Fact]
    public void PlaylistVideo_HasCorrectDefaults()
    {
        // Arrange
        var video = new PlaylistVideo
        {
            Position = 5,
            VideoId = "abc123",
            Title = "Test Video",
            Author = "Test Channel",
            ThumbnailUrl = "https://thumb.jpg",
            PublishedAt = "2024-01-01"
        };

        // Assert
        video.Position.Should().Be(5);
        video.VideoId.Should().Be("abc123");
        video.Title.Should().Be("Test Video");
        video.Author.Should().Be("Test Channel");
    }

    private class HttpMessageHandlerMock : HttpMessageHandler
    {
        private string _responseContent = "{}";
        private HttpStatusCode _statusCode = HttpStatusCode.OK;
        private Queue<(string Content, HttpStatusCode Status)>? _responseSequence;
        public int RequestCount { get; private set; }

        public void SetResponse(string content, HttpStatusCode statusCode)
        {
            _responseContent = content;
            _statusCode = statusCode;
            _responseSequence = null;
        }

        public void SetResponseSequence(IEnumerable<(string Content, HttpStatusCode Status)> sequence)
        {
            _responseSequence = new Queue<(string, HttpStatusCode)>(sequence);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            
            if (_responseSequence != null && _responseSequence.Count > 0)
            {
                var (content, status) = _responseSequence.Dequeue();
                return Task.FromResult(new HttpResponseMessage(status)
                {
                    Content = new StringContent(content)
                });
            }

            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseContent)
            });
        }
    }
}
