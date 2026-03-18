using Bunit;
using FluentAssertions;
using Frontend.Components.Pages;
using Microsoft.AspNetCore.Http;

namespace Frontend.UnitTests;

public class ErrorTests : TestContext
{
    [Fact]
    public void Error_Renders_ErrorMessage()
    {
        // Act
        var cut = RenderComponent<Error>();

        // Assert
        cut.Find("h1").TextContent.Should().Be("Error.");
        cut.Find("h2").TextContent.Should().Be("An error occurred while processing your request.");
    }

    [Fact]
    public void Error_Renders_DevelopmentModeInfo()
    {
        // Act
        var cut = RenderComponent<Error>();

        // Assert
        cut.Find("h3").TextContent.Should().Be("Development Mode");
        cut.Markup.Should().Contain("ASPNETCORE_ENVIRONMENT");
    }

    [Fact]
    public void Error_WithRequestId_ShowsRequestId()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "test-request-id-123";
        
        // Act
        var cut = RenderComponent<Error>(parameters => parameters
            .AddCascadingValue(httpContext));

        // Assert
        cut.Markup.Should().Contain("Request ID:");
        cut.Markup.Should().Contain("test-request-id-123");
    }

    [Fact]
    public void Error_WithoutRequestId_HidesRequestIdSection()
    {
        // Act
        var cut = RenderComponent<Error>();

        // Assert
        cut.Markup.Should().NotContain("Request ID:");
    }

    [Fact]
    public void Error_Has_TextDangerClass()
    {
        // Act
        var cut = RenderComponent<Error>();

        // Assert
        cut.Find("h1").ClassList.Should().Contain("text-danger");
        cut.Find("h2").ClassList.Should().Contain("text-danger");
    }
}
