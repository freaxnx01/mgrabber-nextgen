using DownloadApi;
using DownloadApi.Data;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace DownloadApi.UnitTests;

public class JobTests
{
    [Fact]
    public void Job_DefaultValues_AreCorrect()
    {
        // Arrange
        var job = new Job();

        // Assert
        job.Id.Should().NotBeNullOrEmpty();
        job.Status.Should().Be(JobStatus.Pending);
        job.Progress.Should().Be(0);
        job.FileSizeBytes.Should().Be(0);
        job.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        job.CompletedAt.Should().BeNull();
        job.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Job_WithValues_SetsProperties()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var job = new Job
        {
            Id = "test-id",
            UserId = "user@example.com",
            Url = "https://youtube.com/watch?v=test",
            Title = "Test Song",
            Author = "Test Artist",
            Format = "mp3",
            Status = JobStatus.Completed,
            Progress = 100,
            FilePath = "/storage/file.mp3",
            FileSizeBytes = 1024000,
            CreatedAt = now,
            CompletedAt = now,
            ErrorMessage = null
        };

        // Assert
        job.Id.Should().Be("test-id");
        job.UserId.Should().Be("user@example.com");
        job.Url.Should().Be("https://youtube.com/watch?v=test");
        job.Title.Should().Be("Test Song");
        job.Author.Should().Be("Test Artist");
        job.Format.Should().Be("mp3");
        job.Status.Should().Be(JobStatus.Completed);
        job.Progress.Should().Be(100);
        job.FilePath.Should().Be("/storage/file.mp3");
        job.FileSizeBytes.Should().Be(1024000);
    }

    [Fact]
    public void JobStatus_EnumValues_AreCorrect()
    {
        // Assert
        ((int)JobStatus.Pending).Should().Be(0);
        ((int)JobStatus.Processing).Should().Be(1);
        ((int)JobStatus.Completed).Should().Be(2);
        ((int)JobStatus.Failed).Should().Be(3);
    }

    [Theory]
    [InlineData(JobStatus.Pending, "Pending")]
    [InlineData(JobStatus.Processing, "Processing")]
    [InlineData(JobStatus.Completed, "Completed")]
    [InlineData(JobStatus.Failed, "Failed")]
    public void JobStatus_ToString_ReturnsName(JobStatus status, string expected)
    {
        // Assert
        status.ToString().Should().Be(expected);
    }
}

public class ExtractionResultTests
{
    [Fact]
    public void ExtractionResult_DefaultValues_AreCorrect()
    {
        // Arrange
        var result = new ExtractionResult();

        // Assert
        result.Success.Should().BeFalse();
        result.FilePath.Should().BeNull();
        result.FileSizeBytes.Should().Be(0);
        result.Error.Should().BeNull();
        result.OriginalFilename.Should().BeNull();
        result.CorrectedFilename.Should().BeNull();
    }

    [Fact]
    public void ExtractionResult_SuccessfulResult_HasCorrectValues()
    {
        // Arrange
        var result = new ExtractionResult
        {
            Success = true,
            FilePath = "/storage/song.mp3",
            FileSizeBytes = 5242880,
            OriginalFilename = "Song Title (Official Video).mp3",
            CorrectedFilename = "Artist - Song Title.mp3"
        };

        // Assert
        result.Success.Should().BeTrue();
        result.FilePath.Should().Be("/storage/song.mp3");
        result.FileSizeBytes.Should().Be(5242880);
        result.OriginalFilename.Should().Be("Song Title (Official Video).mp3");
        result.CorrectedFilename.Should().Be("Artist - Song Title.mp3");
    }

    [Fact]
    public void ExtractionResult_FailedResult_HasError()
    {
        // Arrange
        var result = new ExtractionResult
        {
            Success = false,
            Error = "Network timeout"
        };

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Network timeout");
    }
}

public class VideoInfoTests
{
    [Fact]
    public void VideoInfo_HasExpectedProperties()
    {
        // Arrange
        var info = new VideoInfo
        {
            Title = "Test Video",
            Author = "Test Channel",
            Duration = "3:45",
            ViewCount = 1000000
        };

        // Assert
        info.Title.Should().Be("Test Video");
        info.Author.Should().Be("Test Channel");
        info.Duration.Should().Be("3:45");
        info.ViewCount.Should().Be(1000000);
    }
}

public class AudioFormatTests
{
    [Theory]
    [InlineData(AudioFormat.Mp3, 0)]
    [InlineData(AudioFormat.Flac, 1)]
    [InlineData(AudioFormat.M4a, 2)]
    [InlineData(AudioFormat.Webm, 3)]
    public void AudioFormat_EnumValues_AreCorrect(AudioFormat format, int expectedValue)
    {
        // Assert
        ((int)format).Should().Be(expectedValue);
    }

    [Fact]
    public void AudioFormat_AllFormats_AreDefined()
    {
        // Assert
        Enum.GetValues<AudioFormat>().Should().Contain(AudioFormat.Mp3);
        Enum.GetValues<AudioFormat>().Should().Contain(AudioFormat.Flac);
        Enum.GetValues<AudioFormat>().Should().Contain(AudioFormat.M4a);
        Enum.GetValues<AudioFormat>().Should().Contain(AudioFormat.Webm);
    }
}
