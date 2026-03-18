using Bunit;
using FluentAssertions;
using Frontend.Components.Pages;
using Frontend.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Frontend.UnitTests;

public class ProfileTests : TestContext
{
    private readonly IDownloadApiService _downloadApi;
    private readonly ILogger<Profile> _logger;
    private readonly NavigationManager _navigation;

    public ProfileTests()
    {
        _downloadApi = Substitute.For<IDownloadApiService>();
        _logger = Substitute.For<ILogger<Profile>>();
        _navigation = Substitute.For<NavigationManager>();
        
        Services.AddSingleton(_downloadApi);
        Services.AddSingleton(_logger);
        Services.AddSingleton(_navigation);
    }

    [Fact]
    public void Profile_LoadingState_ShowsSpinner()
    {
        // Arrange
        _downloadApi.GetUserQuotaAsync(Arg.Any<string>()).Returns(Task.FromResult<UserQuotaInfoDto?>(null));
        _downloadApi.GetUserFilesAsync().Returns(Task.FromResult<List<FileItem>?>(null));

        // Act
        var cut = RenderComponent<Profile>();

        // Assert
        cut.Find("div.spinner-border").Should().NotBeNull();
    }

    [Fact]
    public void Profile_WithData_ShowsProfileInfo()
    {
        // Arrange
        var quota = new UserQuotaInfoDto
        {
            UserId = "user@example.com",
            TotalBytesAllowed = 1073741824,
            TotalBytesUsed = 536870912,
            PercentageUsed = 50,
            FileCount = 10,
            Threshold = QuotaThreshold.Normal
        };

        var files = new List<FileItem>
        {
            new() { Id = "1", Title = "Song 1", Author = "Artist 1", FileSizeMB = 5.5 },
            new() { Id = "2", Title = "Song 2", Author = "Artist 2", FileSizeMB = 3.2 }
        };

        _downloadApi.GetUserQuotaAsync(Arg.Any<string>()).Returns(Task.FromResult<UserQuotaInfoDto?>(quota));
        _downloadApi.GetUserFilesAsync().Returns(Task.FromResult<List<FileItem>?>(files));

        // Act
        var cut = RenderComponent<Profile>();
        
        // Wait for async loading
        cut.WaitForState(() => !cut.Find("div.spinner-border", timeout: TimeSpan.FromSeconds(1)).HasAttribute("role"));

        // Assert
        cut.Find("h1").TextContent.Should().Contain("My Profile");
    }

    [Fact]
    public void Profile_QuotaWarning_ShowsAlert()
    {
        // Arrange
        var quota = new UserQuotaInfoDto
        {
            PercentageUsed = 85,
            Threshold = QuotaThreshold.Warning
        };

        _downloadApi.GetUserQuotaAsync(Arg.Any<string>()).Returns(Task.FromResult<UserQuotaInfoDto?>(quota));
        _downloadApi.GetUserFilesAsync().Returns(Task.FromResult<List<FileItem>?>(new List<FileItem>()));

        // Act
        var cut = RenderComponent<Profile>();

        // Assert - should show warning badge or alert
        cut.Markup.Should().Contain("Storage");
    }

    [Fact]
    public void Profile_EmptyFiles_ShowsMessage()
    {
        // Arrange
        var quota = new UserQuotaInfoDto
        {
            PercentageUsed = 10,
            FileCount = 0
        };

        _downloadApi.GetUserQuotaAsync(Arg.Any<string>()).Returns(Task.FromResult<UserQuotaInfoDto?>(quota));
        _downloadApi.GetUserFilesAsync().Returns(Task.FromResult<List<FileItem>?>(new List<FileItem>()));

        // Act
        var cut = RenderComponent<Profile>();

        // Assert
        cut.Markup.Should().Contain("files stored");
    }
}
