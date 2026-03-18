using Bunit;
using FluentAssertions;
using Frontend.Components.Pages;
using Frontend.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Frontend.UnitTests;

public class HomeTests : TestContext
{
    private readonly IDownloadApiService _downloadApi = Substitute.For<IDownloadApiService>();
    private readonly ILogger<Home> _logger = Substitute.For<ILogger<Home>>();

    public HomeTests()
    {
        Services.AddSingleton(_downloadApi);
        Services.AddSingleton(_logger);
    }

    [Fact]
    public void Home_Renders_TitleAndDescription()
    {
        // Act
        var cut = RenderComponent<Home>();

        // Assert
        cut.Find("h1").TextContent.Should().Be("🎵 Music Downloader");
        cut.Find("p.text-muted").TextContent.Should().Be("Search and download music from YouTube");
    }

    [Fact]
    public void Home_Renders_SearchSection()
    {
        // Act
        var cut = RenderComponent<Home>();

        // Assert
        cut.Find("input[placeholder='Search for songs, artists...']").Should().NotBeNull();
        cut.Find("button.btn-primary").TextContent.Trim().Should().Be("Search");
    }

    [Fact]
    public void Home_Renders_DownloadsSection()
    {
        // Act
        var cut = RenderComponent<Home>();

        // Assert
        cut.Find("h5").TextContent.Should().Be("Search YouTube");
        cut.FindAll("h5")[1].TextContent.Should().Be("Your Downloads");
    }

    [Fact]
    public void Home_EmptySearchQuery_DoesNotCallApi()
    {
        // Arrange
        var cut = RenderComponent<Home>();
        
        // Act
        var searchButton = cut.Find("button.btn-primary");
        searchButton.Click();

        // Assert
        _downloadApi.DidNotReceive().SearchYouTubeAsync(Arg.Any<string>());
    }

    [Fact]
    public void Home_Renders_NoDownloadsMessage()
    {
        // Act
        var cut = RenderComponent<Home>();

        // Assert
        cut.Find("p.text-muted").TextContent.Should().Contain("No downloads yet");
    }
}
