using Bunit;
using FluentAssertions;
using Frontend.Components.Layout;

namespace Frontend.UnitTests;

public class NavMenuTests : TestContext
{
    [Fact]
    public void NavMenu_Renders_BrandName()
    {
        // Act
        var cut = RenderComponent<NavMenu>();

        // Assert
        cut.Find("a.navbar-brand").TextContent.Should().Contain("Music Downloader");
    }

    [Fact]
    public void NavMenu_Renders_HomeLink()
    {
        // Act
        var cut = RenderComponent<NavMenu>();

        // Assert
        var homeLink = cut.FindAll("a.nav-link")[0];
        homeLink.TextContent.Should().Contain("Home");
        homeLink.GetAttribute("href").Should().Be("");
    }

    [Fact]
    public void NavMenu_Renders_StatisticsLink()
    {
        // Act
        var cut = RenderComponent<NavMenu>();

        // Assert
        var statsLink = cut.FindAll("a.nav-link")
            .FirstOrDefault(l => l.TextContent.Contains("Statistics"));
        statsLink.Should().NotBeNull();
        statsLink!.GetAttribute("href").Should().Be("admin/statistics");
    }

    [Fact]
    public void NavMenu_Renders_WhitelistLink()
    {
        // Act
        var cut = RenderComponent<NavMenu>();

        // Assert
        var whitelistLink = cut.FindAll("a.nav-link")
            .FirstOrDefault(l => l.TextContent.Contains("Whitelist"));
        whitelistLink.Should().NotBeNull();
        whitelistLink!.GetAttribute("href").Should().Be("admin/whitelist");
    }

    [Fact]
    public void NavMenu_Renders_MusicBrainzLink()
    {
        // Act
        var cut = RenderComponent<NavMenu>();

        // Assert
        var mbLink = cut.FindAll("a.nav-link")
            .FirstOrDefault(l => l.TextContent.Contains("MusicBrainz"));
        mbLink.Should().NotBeNull();
        mbLink!.GetAttribute("href").Should().Be("musicbrainz");
    }

    [Fact]
    public void NavMenu_Renders_PlaylistLink()
    {
        // Act
        var cut = RenderComponent<NavMenu>();

        // Assert
        var playlistLink = cut.FindAll("a.nav-link")
            .FirstOrDefault(l => l.TextContent.Contains("Playlist"));
        playlistLink.Should().NotBeNull();
        playlistLink!.GetAttribute("href").Should().Be("playlist");
    }

    [Fact]
    public void NavMenu_Renders_RadioLink()
    {
        // Act
        var cut = RenderComponent<NavMenu>();

        // Assert
        var radioLink = cut.FindAll("a.nav-link")
            .FirstOrDefault(l => l.TextContent.Contains("Radio"));
        radioLink.Should().NotBeNull();
        radioLink!.GetAttribute("href").Should().Be("radio");
    }

    [Fact]
    public void NavMenu_Has_ToggleCheckbox()
    {
        // Act
        var cut = RenderComponent<NavMenu>();

        // Assert
        var toggle = cut.Find("input.navbar-toggler");
        toggle.Should().NotBeNull();
        toggle.GetAttribute("type").Should().Be("checkbox");
    }

    [Fact]
    public void NavMenu_Has_NavScrollableContainer()
    {
        // Act
        var cut = RenderComponent<NavMenu>();

        // Assert
        var scrollable = cut.Find("div.nav-scrollable");
        scrollable.Should().NotBeNull();
    }

    [Fact]
    public void NavMenu_HasSevenNavItems()
    {
        // Act
        var cut = RenderComponent<NavMenu>();

        // Assert
        var navLinks = cut.FindAll("a.nav-link");
        navLinks.Count.Should().Be(7);
    }
}
