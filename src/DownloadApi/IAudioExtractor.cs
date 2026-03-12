namespace DownloadApi;

public interface IAudioExtractor
{
    Task<ExtractionResult> ExtractAsync(string url, AudioFormat format, CancellationToken ct);
    Task<VideoInfo> GetInfoAsync(string url, CancellationToken ct);
    Task<string> GetVersionAsync();
}

public class ExtractionResult
{
    public bool Success { get; set; }
    public string? FilePath { get; set; }
    public string? Error { get; set; }
    public long FileSizeBytes { get; set; }
}

public class VideoInfo
{
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public TimeSpan Duration { get; set; }
    public string ThumbnailUrl { get; set; } = "";
}

public enum AudioFormat
{
    Mp3,
    Flac,
    M4a,
    Webm
}
