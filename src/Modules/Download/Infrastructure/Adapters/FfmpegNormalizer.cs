using System.Diagnostics;
using Microsoft.Extensions.Logging;
using MusicGrabber.Modules.Download.Application.Ports.Driven;

namespace MusicGrabber.Modules.Download.Infrastructure.Adapters;

public sealed class FfmpegNormalizer(ILogger<FfmpegNormalizer> logger) : IAudioNormalizer
{
    public async Task<NormalizationResult> NormalizeAsync(string inputPath, string outputPath, int targetLufs = -14, CancellationToken ct = default)
    {
        try
        {
            logger.LogInformation("Starting audio normalization: {InputPath} -> {OutputPath}", inputPath, outputPath);

            // Pass 1: Analyze loudness
            var analyzeArgs = $"-i \"{inputPath}\" -af loudnorm=I={targetLufs}:TP=-1.5:LRA=11:print_format=json -f null -";
            var (exitCode1, output1) = await RunFfmpegAsync(analyzeArgs, ct);
            if (exitCode1 != 0)
                return new NormalizationResult(false, inputPath, $"Analysis failed: {output1}");

            // Pass 2: Apply normalization
            var normalizeArgs = $"-i \"{inputPath}\" -af loudnorm=I={targetLufs}:TP=-1.5:LRA=11 -y \"{outputPath}\"";
            var (exitCode2, output2) = await RunFfmpegAsync(normalizeArgs, ct);
            if (exitCode2 != 0)
                return new NormalizationResult(false, inputPath, $"Normalization failed: {output2}");

            logger.LogInformation("Normalization complete: {OutputPath}", outputPath);
            return new NormalizationResult(true, outputPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Normalization failed for {InputPath}", inputPath);
            return new NormalizationResult(false, inputPath, ex.Message);
        }
    }

    private static async Task<(int ExitCode, string Output)> RunFfmpegAsync(string args, CancellationToken ct)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var stderr = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        return (process.ExitCode, stderr);
    }
}
