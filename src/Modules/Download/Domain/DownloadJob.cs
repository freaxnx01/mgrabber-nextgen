namespace MusicGrabber.Modules.Download.Domain;

public sealed class DownloadJob
{
    private const int MaxRetries = 3;

    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;
    public string? VideoId { get; private set; }
    public string? Title { get; private set; }
    public string? Author { get; private set; }
    public AudioFormat Format { get; private set; }
    public DownloadStatus Status { get; private set; }
    public int Progress { get; private set; }
    public string? OriginalFilename { get; private set; }
    public string? CorrectedFilename { get; private set; }
    public string? FilePath { get; private set; }
    public long FileSizeBytes { get; private set; }
    public bool NormalizeAudio { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int RetryCount { get; private set; }
    public string? PlaylistId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public bool CanRetry => RetryCount < MaxRetries;

    private DownloadJob() { } // EF Core

    public static DownloadJob Create(string url, string userId, AudioFormat format,
        string? title = null, string? author = null, bool normalizeAudio = false,
        string? playlistId = null)
    {
        return new DownloadJob
        {
            Id = Guid.NewGuid(),
            Url = url,
            UserId = userId,
            Format = format,
            Title = title,
            Author = author,
            NormalizeAudio = normalizeAudio,
            PlaylistId = playlistId,
            Status = DownloadStatus.Pending,
            Progress = 0,
            RetryCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void MarkDownloading()
    {
        Status = DownloadStatus.Downloading;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkNormalizing()
    {
        Status = DownloadStatus.Normalizing;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateProgress(int progress)
    {
        Progress = Math.Clamp(progress, 0, 100);
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkCompleted(string filePath, long fileSizeBytes)
    {
        Status = DownloadStatus.Completed;
        FilePath = filePath;
        FileSizeBytes = fileSizeBytes;
        Progress = 100;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string errorMessage)
    {
        Status = DownloadStatus.Failed;
        ErrorMessage = errorMessage;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementRetry()
    {
        RetryCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateFilenames(string originalFilename, string correctedFilename)
    {
        OriginalFilename = originalFilename;
        CorrectedFilename = correctedFilename;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetVideoId(string videoId)
    {
        VideoId = videoId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetMetadata(string title, string author)
    {
        Title = title;
        Author = author;
        UpdatedAt = DateTime.UtcNow;
    }
}
