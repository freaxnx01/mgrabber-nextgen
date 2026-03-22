using MusicGrabber.Modules.Download.Domain;

namespace MusicGrabber.Modules.Download.UnitTests.Domain;

public class DownloadJobTests
{
    [Fact]
    public void Create_SetsInitialState()
    {
        var job = DownloadJob.Create("https://youtube.com/watch?v=abc", "user1", AudioFormat.Mp3);

        job.Url.Should().Be("https://youtube.com/watch?v=abc");
        job.UserId.Should().Be("user1");
        job.Format.Should().Be(AudioFormat.Mp3);
        job.Status.Should().Be(DownloadStatus.Pending);
        job.Progress.Should().Be(0);
        job.RetryCount.Should().Be(0);
    }

    [Fact]
    public void MarkDownloading_ChangesStatus()
    {
        var job = DownloadJob.Create("https://youtube.com/watch?v=abc", "user1", AudioFormat.Mp3);

        job.MarkDownloading();

        job.Status.Should().Be(DownloadStatus.Downloading);
    }

    [Fact]
    public void MarkCompleted_SetsFileInfo()
    {
        var job = DownloadJob.Create("https://youtube.com/watch?v=abc", "user1", AudioFormat.Mp3);

        job.MarkCompleted("/storage/file.mp3", 1024);

        job.Status.Should().Be(DownloadStatus.Completed);
        job.FilePath.Should().Be("/storage/file.mp3");
        job.FileSizeBytes.Should().Be(1024);
        job.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkFailed_SetsErrorMessage()
    {
        var job = DownloadJob.Create("https://youtube.com/watch?v=abc", "user1", AudioFormat.Mp3);

        job.MarkFailed("Extraction failed");

        job.Status.Should().Be(DownloadStatus.Failed);
        job.ErrorMessage.Should().Be("Extraction failed");
    }

    [Fact]
    public void IncrementRetry_IncrementsCount()
    {
        var job = DownloadJob.Create("https://youtube.com/watch?v=abc", "user1", AudioFormat.Mp3);

        job.IncrementRetry();
        job.IncrementRetry();

        job.RetryCount.Should().Be(2);
    }

    [Fact]
    public void CanRetry_TrueUnderLimit()
    {
        var job = DownloadJob.Create("https://youtube.com/watch?v=abc", "user1", AudioFormat.Mp3);

        job.CanRetry.Should().BeTrue();
    }

    [Fact]
    public void CanRetry_FalseAtLimit()
    {
        var job = DownloadJob.Create("https://youtube.com/watch?v=abc", "user1", AudioFormat.Mp3);
        job.IncrementRetry();
        job.IncrementRetry();
        job.IncrementRetry();

        job.CanRetry.Should().BeFalse();
    }

    [Fact]
    public void UpdateFilenames_SetsOriginalAndCorrected()
    {
        var job = DownloadJob.Create("https://youtube.com/watch?v=abc", "user1", AudioFormat.Mp3);

        job.UpdateFilenames("Artist - Title [Official Video].mp3", "Artist - Title.mp3");

        job.OriginalFilename.Should().Be("Artist - Title [Official Video].mp3");
        job.CorrectedFilename.Should().Be("Artist - Title.mp3");
    }
}
