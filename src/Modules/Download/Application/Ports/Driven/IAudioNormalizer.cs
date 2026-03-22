namespace MusicGrabber.Modules.Download.Application.Ports.Driven;

public interface IAudioNormalizer
{
    Task<NormalizationResult> NormalizeAsync(string inputPath, string outputPath, int targetLufs = -14, CancellationToken ct = default);
}

public sealed record NormalizationResult(bool Success, string OutputPath, string? Error = null);
