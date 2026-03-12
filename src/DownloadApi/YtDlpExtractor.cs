using System.Diagnostics;

namespace DownloadApi;

public class YtDlpExtractor : IAudioExtractor
{
    private readonly ILogger<YtDlpExtractor> _logger;
    private readonly string _ytDlpPath = "/usr/local/bin/yt-dlp";
    private readonly string _storagePath = "/storage";

    public YtDlpExtractor(ILogger<YtDlpExtractor> logger)
    {
        _logger = logger;
    }

    public async Task<string> GetVersionAsync()
    {
        try
        {
            if (!File.Exists(_ytDlpPath))
            {
                return "yt-dlp not found";
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _ytDlpPath,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };

            process.Start();
            var version = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return version.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get yt-dlp version");
            return $"Error: {ex.Message}";
        }
    }

    public async Task<VideoInfo> GetInfoAsync(string url, CancellationToken ct)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _ytDlpPath,
                    Arguments = $"--dump-json --no-download {url}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };

            process.Start();
            var json = await process.StandardOutput.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            // TODO: Parse JSON and return VideoInfo
            return new VideoInfo { Title = "Parsing not implemented yet" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get video info");
            throw;
        }
    }

    public async Task<ExtractionResult> ExtractAsync(string url, AudioFormat format, CancellationToken ct)
    {
        try
        {
            var formatArg = format switch
            {
                AudioFormat.Mp3 => "mp3",
                AudioFormat.Flac => "flac",
                AudioFormat.M4a => "m4a",
                AudioFormat.Webm => "webm",
                _ => "mp3"
            };

            var outputPath = $"{_storagePath}/%(title)s.%(ext)s";
            
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _ytDlpPath,
                    Arguments = $"-x --audio-format {formatArg} -o {outputPath} {url}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };

            process.Start();
            await process.WaitForExitAsync(ct);

            // TODO: Get actual file path and size
            return new ExtractionResult
            {
                Success = process.ExitCode == 0,
                FilePath = outputPath,
                Error = process.ExitCode != 0 ? await process.StandardError.ReadToEndAsync(ct) : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract audio");
            return new ExtractionResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }
}
