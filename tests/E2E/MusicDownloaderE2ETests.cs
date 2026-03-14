using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace E2E;

[Parallelizable(ParallelScope.Self)]
public sealed class MusicDownloaderE2ETests : PageTest
{
    private const string BaseUrl = "http://localhost:8086";

    [SetUp]
    public async Task SetUp()
    {
        await Page.GotoAsync(BaseUrl);
    }

    [Test]
    public async Task HomePage_LoadsSuccessfully()
    {
        // Assert
        await Expect(Page).ToHaveTitleAsync(new Regex("Music Downloader"));
        await Expect(Page.Locator("h1")).ToContainTextAsync("Music Downloader");
    }

    [Test]
    public async Task SearchForSong_ShowsResults()
    {
        // Arrange
        var searchInput = Page.Locator("input[placeholder*='Search']");
        var searchButton = Page.Locator("button:has-text('Search')");

        // Act
        await searchInput.FillAsync("roxette the look");
        await searchButton.ClickAsync();

        // Assert
        await Expect(Page.Locator(".list-group")).ToBeVisibleAsync();
        await Expect(Page.Locator(".list-group-item")).ToHaveCountAsync(2);
    }

    [Test]
    public async Task DownloadSong_StartsDownload()
    {
        // Arrange
        await Page.GotoAsync(BaseUrl);
        await Page.Locator("input[placeholder*='Search']").FillAsync("roxette");
        await Page.Locator("button:has-text('Search')").ClickAsync();

        // Act
        var downloadButton = Page.Locator("button:has-text('Download')").First;
        await downloadButton.ClickAsync();

        // Assert - Check for success message or status
        await Expect(Page.Locator("text=Download started")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Navigation_ToStatisticsPage()
    {
        // Act
        await Page.ClickAsync("text=Statistics");

        // Assert
        await Expect(Page).ToHaveURLAsync(new Regex("/admin/statistics"));
        await Expect(Page.Locator("h1")).ToContainTextAsync("Statistics");
    }

    [Test]
    public async Task StatisticsPage_ShowsCards()
    {
        // Arrange
        await Page.GotoAsync($"{BaseUrl}/admin/statistics");

        // Assert - Check for stats cards
        await Expect(Page.Locator("text=Total Downloads")).ToBeVisibleAsync();
        await Expect(Page.Locator("text=Storage Used")).ToBeVisibleAsync();
        await Expect(Page.Locator("text=Active Users")).ToBeVisibleAsync();
    }

    [Test]
    public async Task DownloadFlow_FullUserJourney()
    {
        // Step 1: Search
        await Page.GotoAsync(BaseUrl);
        await Page.Locator("input[placeholder*='Search']").FillAsync("roxette listen to your heart");
        await Page.Locator("button:has-text('Search')").ClickAsync();

        // Step 2: Verify results appear
        await Expect(Page.Locator(".list-group")).ToBeVisibleAsync();

        // Step 3: Click download
        await Page.Locator("button:has-text('Download')").First.ClickAsync();

        // Step 4: Verify success message
        await Expect(Page.Locator("text=Download started")).ToBeVisibleAsync();

        // Step 5: Navigate to downloads (if visible)
        // Note: Downloads section shows on same page
        await Expect(Page.Locator("text=Your Downloads")).ToBeVisibleAsync();
    }
}
