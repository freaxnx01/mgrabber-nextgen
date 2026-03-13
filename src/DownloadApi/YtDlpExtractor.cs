using System.Diagnostics;
using System.Text.RegularExpressions;

namespace DownloadApi;

public class YtDlpExtractor : IAudioExtractor
{
    private readonly ILogger<YtDlpExtractor> _logger;
    private readonly string _ytDlpPath = "yt-dlp";
    private readonly string _storagePath = "/storage";

    public YtDlpExtractor(ILogger<YtDlpExtractor> logger)
    {
        _logger = logger;
    }

    public async Task<string> GetVersionAsync()
    {
        try
        {
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
        // Basic extraction without custom filename (fallback)
        return await ExtractAsync(url, format, null, null, null, null, ct);
    }

    public async Task<ExtractionResult> ExtractAsync(
        string url, 
        AudioFormat format, 
        string? userId,
        string? jobId,
        string? author,
        string? title,
        CancellationToken ct)
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

            // Build output path
            string outputDir;
            string outputTemplate;
            
            if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(jobId))
            {
                // Structured path: /storage/{userId}/{jobId}/
                outputDir = Path.Combine(_storagePath, SanitizePath(userId), jobId);
                Directory.CreateDirectory(outputDir);
                
                // Custom filename: Author - Title.mp3
                if (!string.IsNullOrEmpty(author) && !string.IsNullOrEmpty(title))
                {
                    var filename = $"{SanitizeFilename(author)} - {SanitizeFilename(title)}.%(ext)s";
                    outputTemplate = Path.Combine(outputDir, filename);
                }
                else
                {
                    // Fallback to yt-dlp default with title
                    outputTemplate = Path.Combine(outputDir, "%(title)s.%(ext)s");
                }
            }
            else
            {
                // Fallback to root storage
                outputTemplate = Path.Combine(_storagePath, "%(title)s.%(ext)s");
                outputDir = _storagePath;
            }

            _logger.LogInformation("Starting extraction: {Url} -> {Output}", url, outputTemplate);
            
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _ytDlpPath,
                    Arguments = $"-x --audio-format {formatArg} -o \"{outputTemplate}\" {url}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(ct);
            var error = await process.StandardError.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            if (process.ExitCode == 0)
            {
                // Find the actual downloaded file
                var downloadedFile = FindDownloadedFile(outputDir, formatArg);
                var fileSize = downloadedFile != null ? new FileInfo(downloadedFile).Length : 0;

                // Get the original filename from the output
                var originalFilename = ExtractOriginalFilename(output);
                var correctedFilename = Path.GetFileName(downloadedFile);

                return new ExtractionResult
                {
                    Success = true,
                    FilePath = downloadedFile,
                    FileSizeBytes = fileSize,
                    OriginalFilename = originalFilename,
                    CorrectedFilename = correctedFilename
                };
            }
            else
            {
                _logger.LogError("yt-dlp failed: {Error}", error);
                return new ExtractionResult
                {
                    Success = false,
                    Error = error
                };
            }
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

    private string? ExtractOriginalFilename(string ytDlpOutput)
    {
        // Try to extract the destination filename from yt-dlp output
        // Example: "[download] Destination: /path/to/filename.mp3"
        var lines = ytDlpOutput.Split('\n');
        foreach (var line in lines)
        {
            if (line.Contains("Destination:") || line.Contains("[download]"))
            {
                // Extract filename from the line
                var match = System.Text.RegularExpressions.Regex.Match(line, @"Destination:\s*(.+)$");
                if (match.Success)
                {
                    var fullPath = match.Groups[1].Value.Trim();
                    return Path.GetFileName(fullPath);
                }
            }
        }
        return null;
    }

    private string? FindDownloadedFile(string directory, string format)
    {
        if (!Directory.Exists(directory))
            return null;

        // Look for files with the target extension
        var extension = $".{format}";
        var files = Directory.GetFiles(directory, $"*{extension}");
        
        if (files.Length > 0)
        {
            // Return the most recently created file
            return files.Select(f => new FileInfo(f))
                       .OrderByDescending(f => f.CreationTime)
                       .First()
                       .FullName;
        }

        return null;
    }

    private string SanitizeFilename(string filename)
    {
        // Remove or replace invalid filename characters
        var invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
        var sanitized = filename;
        
        foreach (char c in invalid)
        {
            sanitized = sanitized.Replace(c, '-');
        }
        
        // Trim and limit length
        sanitized = sanitized.Trim();
        if (sanitized.Length > 100)
        {
            sanitized = sanitized.Substring(0, 100);
        }
        
        return sanitized;
    }

    private string SanitizePath(string path)
    {
        // Remove or replace invalid path characters
        var invalid = Path.GetInvalidPathChars();
        var sanitized = path;
        
        foreach (char c in invalid)
        {
            sanitized = sanitized.Replace(c, '-');
        }
        
        return sanitized.Trim();
    }
}
