using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using MusicGrabber.Modules.Download.Application.Ports.Driven;

namespace MusicGrabber.Modules.Download.Infrastructure.Adapters;

public sealed partial class YtDlpExtractor(ILogger<YtDlpExtractor> logger) : IAudioExtractor
{
    public async Task<ExtractionResult> ExtractAsync(string url, string format, string outputDir,
        string? userId = null, string? jobId = null, string? author = null, string? title = null,
        CancellationToken ct = default)
    {
        var outputTemplate = Path.Combine(outputDir, "%(title)s.%(ext)s");
        var formatArg = format.ToLowerInvariant() switch
        {
            "mp3" => "mp3",
            "flac" => "flac",
            "m4a" => "m4a",
            "webm" => "webm",
            _ => "mp3"
        };

        var args = $"-x --audio-format {formatArg} --audio-quality 0 -o \"{outputTemplate}\" \"{url}\"";
        logger.LogInformation("Starting yt-dlp extraction: {Url} -> {Format}", url, formatArg);
        var (exitCode, output) = await RunProcessAsync("yt-dlp", args, ct);

        if (exitCode != 0)
            throw new InvalidOperationException($"yt-dlp failed with exit code {exitCode}: {output}");

        var filePath = FindOutputFile(output, outputDir, formatArg);
        if (filePath is null || !File.Exists(filePath))
            throw new InvalidOperationException("yt-dlp completed but output file not found.");

        var originalFilename = Path.GetFileName(filePath);
        var correctedFilename = CleanFilename(originalFilename);

        if (originalFilename != correctedFilename)
        {
            var correctedPath = Path.Combine(outputDir, correctedFilename);
            File.Move(filePath, correctedPath);
            filePath = correctedPath;
        }

        var fileSize = new FileInfo(filePath).Length;
        logger.LogInformation("Extraction complete: {FilePath} ({FileSize} bytes)", filePath, fileSize);
        return new ExtractionResult(filePath, originalFilename, correctedFilename, fileSize);
    }

    public async Task<VideoInfo> GetInfoAsync(string url, CancellationToken ct = default)
    {
        var args = $"--dump-json --no-download \"{url}\"";
        var (exitCode, output) = await RunProcessAsync("yt-dlp", args, ct);

        if (exitCode != 0)
            throw new InvalidOperationException($"yt-dlp info failed: {output}");

        var json = System.Text.Json.JsonDocument.Parse(output);
        var root = json.RootElement;

        return new VideoInfo(
            VideoId: root.GetProperty("id").GetString() ?? "",
            Title: root.GetProperty("title").GetString() ?? "",
            Author: root.TryGetProperty("uploader", out var u) ? u.GetString() ?? "" : "",
            Duration: root.TryGetProperty("duration", out var d) ?
                TimeSpan.FromSeconds(d.GetDouble()).ToString(@"m\:ss") : "",
            ThumbnailUrl: root.TryGetProperty("thumbnail", out var t) ? t.GetString() ?? "" : ""
        );
    }

    public async Task<string> GetVersionAsync()
    {
        var (_, output) = await RunProcessAsync("yt-dlp", "--version", CancellationToken.None);
        return output.Trim();
    }

    private static async Task<(int ExitCode, string Output)> RunProcessAsync(string command, string args, CancellationToken ct)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync(ct);
        var error = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        return (process.ExitCode, string.IsNullOrEmpty(output) ? error : output);
    }

    private static string? FindOutputFile(string output, string outputDir, string format)
    {
        var pattern = @"\[ExtractAudio\] Destination: (.+)";
        var match = Regex.Match(output, pattern);
        if (match.Success)
            return match.Groups[1].Value.Trim();

        return Directory.GetFiles(outputDir, $"*.{format}")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    private static string CleanFilename(string filename)
    {
        var name = Path.GetFileNameWithoutExtension(filename);
        var ext = Path.GetExtension(filename);

        name = FilenameCleanupRegex().Replace(name, "").Trim();
        name = ExtraWhitespaceRegex().Replace(name, " ").Trim();
        name = name.TrimEnd('-', '_', ' ');

        return $"{name}{ext}";
    }

    [GeneratedRegex(@"\s*[\[\(](?:Official\s*(?:Music\s*)?Video|Lyric(?:s)?\s*Video|Audio|HD|HQ|4K|MV|Live|Visuali[sz]er|Official\s*Audio|feat\.?[^\]\)]*|ft\.?[^\]\)]*)[\]\)]", RegexOptions.IgnoreCase)]
    private static partial Regex FilenameCleanupRegex();

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex ExtraWhitespaceRegex();
}
