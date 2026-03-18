using Bunit;
using FluentAssertions;
using Frontend.Components.Pages;
using Frontend.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Frontend.UnitTests;

public class RadioTests : TestContext
{
    private readonly IDownloadApiService _downloadApi;
    private readonly ILogger<Radio> _logger;

    public RadioTests()
    {
        _downloadApi = Substitute.For<IDownloadApiService>();
        _logger = Substitute.For<ILogger<Radio>>();
        
        Services.AddSingleton(_downloadApi);
        Services.AddSingleton(_logger);
    }

    [Fact]
    public void Radio_Renders_Title()
    {
        // Arrange
        var stations = new List<RadioStationDto>();
        _downloadApi.GetRadioStationsAsync().Returns(Task.FromResult(stations));

        // Act
        var cut = RenderComponent<Radio>();

        // Assert
        cut.Find("h1").TextContent.Should().Contain("Radio");
    }

    [Fact]
    public void Radio_LoadingState_ShowsSpinner()
    {
        // Arrange
        _downloadApi.GetRadioStationsAsync().Returns(Task.FromResult(new List<RadioStationDto>()));

        // Act
        var cut = RenderComponent<Radio>();

        // Assert
        cut.Markup.Should().Contain("Loading");
    }

    [Fact]
    public void Radio_WithStations_ShowsStationList()
    {
        // Arrange
        var stations = new List<RadioStationDto>
        {
            new() { Id = "srf1", Name = "Radio SRF 1" },
            new() { Id = "srf3", Name = "Radio SRF 3" }
        };
        _downloadApi.GetRadioStationsAsync().Returns(Task.FromResult(stations));

        // Act
        var cut = RenderComponent<Radio>();

        // Assert
        cut.Markup.Should().Contain("Radio SRF 1");
        cut.Markup.Should().Contain("Radio SRF 3");
    }

    [Fact]
    public void Radio_NoStations_ShowsMessage()
    {
        // Arrange
        _downloadApi.GetRadioStationsAsync().Returns(Task.FromResult(new List<RadioStationDto>()));

        // Act
        var cut = RenderComponent<Radio>();

        // Assert
        cut.Markup.Should().Contain("No radio stations available");
    }
}
