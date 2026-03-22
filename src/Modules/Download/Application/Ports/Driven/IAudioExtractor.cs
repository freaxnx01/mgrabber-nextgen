namespace MusicGrabber.Modules.Download.Application.Ports.Driven;

public interface IAudioExtractor
{
    Task<ExtractionResult> ExtractAsync(string url, string format, string outputDir,
        string? userId = null, string? jobId = null, string? author = null, string? title = null,
        CancellationToken ct = default);
    Task<VideoInfo> GetInfoAsync(string url, CancellationToken ct = default);
    Task<string> GetVersionAsync();
}

public sealed record ExtractionResult(string FilePath, string OriginalFilename, string CorrectedFilename, long FileSizeBytes);
public sealed record VideoInfo(string VideoId, string Title, string Author, string Duration, string ThumbnailUrl);
