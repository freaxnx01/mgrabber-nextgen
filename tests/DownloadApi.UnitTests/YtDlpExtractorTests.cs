using System.Text.RegularExpressions;

namespace DownloadApi.UnitTests;

public sealed class YtDlpExtractorTests
{
    private readonly ILogger<YtDlpExtractor> _logger = Substitute.For<ILogger<YtDlpExtractor>>();

    [Theory]
    [InlineData("https://youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ&feature=share", "dQw4w9WgXcQ")]
    [InlineData("https://youtube.com/v/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://example.com/video", null)]
    [InlineData("invalid-url", null)]
    public void ExtractVideoId_VariousUrls_ReturnsExpectedId(string url, string? expectedId)
    {
        // Act
        var result = JobRepository.ExtractVideoId(url);

        // Assert
        result.Should().Be(expectedId);
    }

    [Fact]
    public void CleanTitle_WithOfficialMusicVideo_RemovesSuffix()
    {
        // Arrange
        var extractor = new YtDlpExtractor(_logger);
        var title = "Song Title (Official Music Video)";

        // Act - using reflection to test private method
        var method = typeof(YtDlpExtractor).GetMethod("CleanTitle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = method!.Invoke(extractor, new object?[] { title });

        // Assert
        result.Should().Be("Song Title");
    }

    [Theory]
    [InlineData("Song (Official Video)", "Song")]
    [InlineData("Song (Music Video)", "Song")]
    [InlineData("Song (Official Audio)", "Song")]
    [InlineData("Song (Lyrics)", "Song")]
    [InlineData("Song (HD)", "Song")]
    [InlineData("Song (Remastered)", "Song")]
    [InlineData("Song (Live)", "Song")]
    [InlineData("Song (Acoustic)", "Song")]
    [InlineData("Song (Explicit)", "Song")]
    [InlineData("Song (Visualizer)", "Song")]
    public void CleanTitle_VariousPatterns_RemovesPromotionalText(string input, string expected)
    {
        // Arrange
        var extractor = new YtDlpExtractor(_logger);
        var method = typeof(YtDlpExtractor).GetMethod("CleanTitle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = method!.Invoke(extractor, new object?[] { input });

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void CleanTitle_MultiplePatterns_RemovesAll()
    {
        // Arrange
        var extractor = new YtDlpExtractor(_logger);
        var title = "Song Title (Official Music Video) (HD) (Remastered)";
        var method = typeof(YtDlpExtractor).GetMethod("CleanTitle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = method!.Invoke(extractor, new object?[] { title });

        // Assert
        result.Should().Be("Song Title");
    }

    [Fact]
    public void CleanTitle_EmptyOrWhitespace_ReturnsOriginal()
    {
        // Arrange
        var extractor = new YtDlpExtractor(_logger);
        var method = typeof(YtDlpExtractor).GetMethod("CleanTitle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act & Assert
        method!.Invoke(extractor, new object?[] { "" }).Should().Be("");
        method!.Invoke(extractor, new object?[] { "   " }).Should().Be("   ");
        method!.Invoke(extractor, new object?[] { null }).Should().BeNull();
    }

    [Fact]
    public void SanitizeFilename_WithInvalidChars_ReplacesWithDash()
    {
        // Arrange
        var extractor = new YtDlpExtractor(_logger);
        var method = typeof(YtDlpExtractor).GetMethod("SanitizeFilename", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var filename = "Artist <Name>: Song|Title?*.mp3";

        // Act
        var result = method!.Invoke(extractor, new object[] { filename });

        // Assert
        result.Should().Be("Artist -Name- - Song-Title---.mp3");
    }

    [Fact]
    public void SanitizeFilename_LongName_TruncatesTo100Chars()
    {
        // Arrange
        var extractor = new YtDlpExtractor(_logger);
        var method = typeof(YtDlpExtractor).GetMethod("SanitizeFilename", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var longName = new string('a', 150);

        // Act
        var result = method!.Invoke(extractor, new object[] { longName });

        // Assert
        result!.ToString()!.Length.Should().Be(100);
    }
}
