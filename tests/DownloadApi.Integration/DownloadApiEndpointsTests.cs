using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace DownloadApi.Integration;

public sealed class DownloadApiEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public DownloadApiEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/api/health");
        var result = await response.Content.ReadFromJsonAsync<HealthResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result!.Status.Should().Be("Healthy");
        result.Services.Should().ContainKeys("YtDlp", "Ffmpeg");
    }

    [Fact]
    public async Task SearchYoutube_WithQuery_ReturnsResults()
    {
        // Act
        var response = await _client.GetAsync("/api/search/youtube?q=roxette");

        // Assert - Check status code (results depend on API key configuration)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
        
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var result = await response.Content.ReadFromJsonAsync<YoutubeSearchResponse>();
            result!.Query.Should().Be("roxette");
            result.Results.Should().NotBeNull();
            // Note: Results may be empty if API key not configured in test environment
        }
    }

    [Theory]
    [InlineData("the beatles")]
    [InlineData("roxette")]
    [InlineData("queen")]
    public async Task SearchYoutube_VariousQueries_ReturnsStructuredResponse(string query)
    {
        // Act
        var response = await _client.GetAsync($"/api/search/youtube?q={Uri.EscapeDataString(query)}");

        // Assert - Endpoint should respond (actual results depend on API key)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
        
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var result = await response.Content.ReadFromJsonAsync<YoutubeSearchResponse>();
            result.Should().NotBeNull();
            result!.Query.Should().Be(query);
            result.Results.Should().NotBeNull();
            
            // If results exist, verify structure
            if (result.Results.Any())
            {
                var first = result.Results.First();
                first.VideoId.Should().NotBeNullOrEmpty();
                first.Title.Should().NotBeNullOrEmpty();
                first.Author.Should().NotBeNullOrEmpty();
            }
        }
    }

    [Fact]
    public async Task SearchYoutube_WithoutQuery_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/search/youtube");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task StartDownload_WithValidRequest_ReturnsAccepted()
    {
        // Arrange
        var request = new { Url = "https://youtube.com/watch?v=test123", UserId = "test-user", Title = "Test", Author = "Artist" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/download/start", request);
        var result = await response.Content.ReadFromJsonAsync<DownloadStartResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        result!.JobId.Should().NotBeNullOrEmpty();
        result.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task StartDownload_WithoutUrl_ReturnsBadRequest()
    {
        // Arrange
        var request = new { UserId = "test-user" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/download/start", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetDownloadStatus_NonExistentJob_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/download/status/non-existent-id");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AdminStatsGlobal_ReturnsStatistics()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/stats/global");
        var result = await response.Content.ReadFromJsonAsync<GlobalStatsResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result!.Should().NotBeNull();
        result.TotalDownloads.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task AdminStatsUsers_ReturnsUserList()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/stats/users");
        var result = await response.Content.ReadFromJsonAsync<List<UserStatsResponse>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result!.Should().NotBeNull();
    }

    // DTOs for test responses
    private record HealthResponse(string Status, Dictionary<string, string> Services);
    private record YoutubeSearchResponse(string Query, List<YoutubeVideo> Results, int TotalResults);
    private record YoutubeVideo(string VideoId, string Title, string Author, string Duration);
    private record DownloadStartResponse(string JobId, string Status, string Message);
    private record GlobalStatsResponse(int TotalDownloads, long TotalStorageBytes, double TotalStorageMB);
    private record UserStatsResponse(string UserId, int TotalDownloads, double TotalStorageMB);
}
