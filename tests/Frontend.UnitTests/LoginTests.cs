using Bunit;
using FluentAssertions;
using Frontend.Components.Pages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Frontend.UnitTests;

public class LoginTests : TestContext
{
    private readonly IConfiguration _configuration;

    public LoginTests()
    {
        _configuration = Substitute.For<IConfiguration>();
        Services.AddSingleton(_configuration);
    }

    [Fact]
    public void Login_GoogleConfigured_ShowsGoogleButton()
    {
        // Arrange
        _configuration["Authentication:Google:ClientId"].Returns("google-client-id-123");

        // Act
        var cut = RenderComponent<Login>();

        // Assert
        cut.Find("a.btn-danger").Should().NotBeNull();
        cut.Find("a.btn-danger").TextContent.Should().Contain("Sign in with Google");
    }

    [Fact]
    public void Login_NoGoogleConfig_ShowsDevMode()
    {
        // Arrange
        _configuration["Authentication:Google:ClientId"].Returns((string?)null);
        _configuration["DevAuth:Email"].Returns("dev@test.local");

        // Act
        var cut = RenderComponent<Login>();

        // Assert
        cut.Find("div.alert-info").Should().NotBeNull();
        cut.Find("div.alert-info").TextContent.Should().Contain("Development Mode");
        cut.Find("div.alert-info").TextContent.Should().Contain("dev@test.local");
        cut.Find("a.btn-primary").TextContent.Should().Contain("Continue to App");
    }

    [Fact]
    public void Login_WithErrorMessage_DisplaysError()
    {
        // Arrange
        _configuration["Authentication:Google:ClientId"].Returns("google-client-id");

        // Act
        var cut = RenderComponent<Login>(parameters => parameters
            .Add(p => p.ErrorMessage, "Authentication failed"));

        // Assert
        cut.Find("div.alert-danger").Should().NotBeNull();
        cut.Find("div.alert-danger").TextContent.Should().Be("Authentication failed");
    }

    [Fact]
    public void Login_Renders_TitleAndDescription()
    {
        // Arrange
        _configuration["Authentication:Google:ClientId"].Returns("google-client-id");

        // Act
        var cut = RenderComponent<Login>();

        // Assert
        cut.Find("h3").TextContent.Should().Contain("Music Grabber");
        cut.Find("p.text-muted").TextContent.Should().Contain("Sign in to continue");
    }

    [Fact]
    public void Login_DefaultDevEmail_UsesFallback()
    {
        // Arrange
        _configuration["Authentication:Google:ClientId"].Returns((string?)null);
        _configuration["DevAuth:Email"].Returns((string?)null);

        // Act
        var cut = RenderComponent<Login>();

        // Assert
        cut.Find("div.alert-info").TextContent.Should().Contain("admin@test.local");
    }
}
