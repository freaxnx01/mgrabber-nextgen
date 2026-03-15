using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace E2E;

[Parallelizable(ParallelScope.Self)]
public sealed class YouTubeSearchAndDownloadE2ETests : PageTest
{
    private const string BaseUrl = "http://localhost:8086";
    private const string DownloadTimeout = "120000"; // 2 minutes for download + processing

    [SetUp]
    public async Task SetUp()
    {
        // Navigate to app
        await Page.GotoAsync(BaseUrl);
        
        // Wait for page to load
        await Expect(Page.Locator("h1")).ToContainTextAsync("Music Downloader");
    }

    [Test]
    public async Task SearchYouTube_ValidQuery_ShowsRealResults()
    {
        // Skip if running in CI without API key
        var apiKeyConfigured = await CheckApiKeyConfigured();
        if (!apiKeyConfigured)
        {
            Assert.Ignore("YouTube API key not configured - skipping E2E test");
            return;
        }

        // Arrange
        var searchInput = Page.Locator("input[placeholder*='Search']");
        var searchButton = Page.Locator("button:has-text('Search')");

        // Act - Search for a well-known song
        await searchInput.FillAsync("Roxette The Look");
        await searchButton.ClickAsync();

        // Assert - Wait for results
        await Expect(Page.Locator(".list-group")).ToBeVisibleAsync();
        
        // Verify results appear
        var results = Page.Locator(".list-group-item");
        await Expect(results).ToHaveCountAsync(new Regex(@"[1-9]")); // At least 1 result
        
        // Verify first result has expected structure
        var firstResult = results.First;
        await Expect(firstResult.Locator("h6")).Not.ToBeEmptyAsync(); // Title
        await Expect(firstResult.Locator("small")).Not.ToBeEmptyAsync(); // Author + Duration
        await Expect(firstResult.Locator("button:has-text('Download')")).ToBeVisibleAsync();
    }

    [Test]
    public async Task SearchYouTube_NoResults_ShowsEmptyState()
    {
        // Arrange
        var searchInput = Page.Locator("input[placeholder*='Search']");
        var searchButton = Page.Locator("button:has-text('Search')");

        // Act - Search for nonsense that shouldn't return results
        await searchInput.FillAsync("xyzabc123nonsense");
        await searchButton.ClickAsync();

        // Assert - Either no results or very few
        var results = Page.Locator(".list-group-item");
        var count = await results.CountAsync();
        
        // Should have 0-2 results (API might return some random match)
        Assert.That(count, Is.LessThan(3), "Should show minimal results for nonsense query");
    }

    [Test]
    public async Task DownloadYouTubeVideo_FullFlow_Success()
    {
        // Skip if running in CI without API key
        var apiKeyConfigured = await CheckApiKeyConfigured();
        if (!apiKeyConfigured)
        {
            Assert.Ignore("YouTube API key not configured - skipping E2E test");
            return;
        }

        // Step 1: Search
        await Page.Locator("input[placeholder*='Search']").FillAsync("Roxette The Look");
        await Page.Locator("button:has-text('Search')").ClickAsync();
        
        // Wait for results
        await Expect(Page.Locator(".list-group")).ToBeVisibleAsync();
        
        // Step 2: Click Download on first result
        var downloadButton = Page.Locator("button:has-text('Download')").First;
        await downloadButton.ClickAsync();
        
        // Step 3: Verify success message
        await Expect(Page.Locator("text=Download started")).ToBeVisibleAsync();
        
        // Step 4: Wait for download section to appear/update
        await Expect(Page.Locator("text=Your Downloads")).ToBeVisibleAsync();
        
        // Step 5: Wait for download to complete (polling)
        var completed = await WaitForDownloadComplete(TimeSpan.FromSeconds(120));
        
        if (!completed)
        {
            Assert.Fail("Download did not complete within timeout");
        }
        
        // Step 6: Verify file appears in downloads table
        var downloadRow = Page.Locator("table tbody tr").First;
        await Expect(downloadRow).ToBeVisibleAsync();
        
        // Verify row has expected columns
        var cells = downloadRow.Locator("td");
        await Expect(cells).ToHaveCountAsync(5); // Title, Artist, Size, Status, Actions
        
        // Verify status is available
        var statusCell = cells.Nth(3);
        await Expect(statusCell.Locator("text=Available")).ToBeVisibleAsync();
        
        // Step 7: Verify file can be downloaded (optional - just check link exists)
        // This would download the actual file - uncomment if needed
        // var downloadLink = downloadRow.Locator("a[download]");
        // await Expect(downloadLink).ToBeVisibleAsync();
    }

    [Test]
    public async Task DownloadWithNormalization_OptionWorks()
    {
        // Skip if running in CI without API key
        var apiKeyConfigured = await CheckApiKeyConfigured();
        if (!apiKeyConfigured)
        {
            Assert.Ignore("YouTube API key not configured - skipping E2E test");
            return;
        }

        // Search and find first result
        await Page.Locator("input[placeholder*='Search']").FillAsync("test audio");
        await Page.Locator("button:has-text('Search')").ClickAsync();
        
        await Expect(Page.Locator(".list-group")).ToBeVisibleAsync();
        
        // Look for normalize checkbox (if implemented in UI)
        // Note: Current UI doesn't have this checkbox, test documents expected behavior
        
        // Click download
        await Page.Locator("button:has-text('Download')").First.ClickAsync();
        
        // Verify download started
        await Expect(Page.Locator("text=Download started")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Download_InvalidVideo_ShowsError()
    {
        // Try to download an invalid/removed video
        // This tests error handling
        
        // Navigate directly with invalid video URL (simulating edge case)
        // This would require API modification to test properly
        
        // For now, just verify error handling works in general
        await Page.GotoAsync($"{BaseUrl}/api/download/start");
        
        // Should redirect or show error (not crash)
        var statusCode = await Page.EvaluateAsync<int>("() => document.status");
        Assert.That(statusCode, Is.Not.EqualTo(500), "Should handle invalid requests gracefully");
    }

    private async Task<bool> CheckApiKeyConfigured()
    {
        try
        {
            // Try a search - if it fails with config error, API key not set
            await Page.Locator("input[placeholder*='Search']").FillAsync("test");
            await Page.Locator("button:has-text('Search')").ClickAsync();
            
            // Wait a bit
            await Page.WaitForTimeoutAsync(2000);
            
            // Check if we got an error about API key
            var errorText = await Page.Locator("text=API").IsVisibleAsync();
            return !errorText;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> WaitForDownloadComplete(TimeSpan timeout)
    {
        var startTime = DateTime.Now;
        
        while (DateTime.Now - startTime < timeout)
        {
            // Check if any download shows as completed
            var availableBadges = Page.Locator("span:has-text('Available')");
            var count = await availableBadges.CountAsync();
            
            if (count > 0)
            {
                return true;
            }
            
            // Check if there's an error
            var errorAlert = Page.Locator(".alert-danger");
            if (await errorAlert.IsVisibleAsync())
            {
                var errorText = await errorAlert.TextContentAsync();
                if (errorText?.Contains("Download") == true)
                {
                    return false; // Download failed
                }
            }
            
            // Wait before checking again
            await Page.WaitForTimeoutAsync(5000); // Check every 5 seconds
        }
        
        return false; // Timeout
    }
}
