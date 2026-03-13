using System.Diagnostics;

namespace DownloadApi;

public class AudioNormalizer
{
    private readonly ILogger<AudioNormalizer> _logger;

    public AudioNormalizer(ILogger<AudioNormalizer> logger)
    {
        _logger = logger;
    }

    public async Task<NormalizationResult> NormalizeAsync(
        string inputPath, 
        string outputPath, 
        NormalizationOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new NormalizationOptions();
        
        try
        {
            _logger.LogInformation("Starting audio normalization for: {Input}", inputPath);

            // Two-pass normalization for best quality
            // Pass 1: Analyze audio and get measured values
            var measuredValues = await RunFirstPassAsync(inputPath, options, ct);
            
            if (measuredValues == null)
            {
                return new NormalizationResult 
                { 
                    Success = false, 
                    Error = "First pass analysis failed" 
                };
            }

            // Pass 2: Apply normalization with measured values
            var success = await RunSecondPassAsync(inputPath, outputPath, options, measuredValues, ct);
            
            if (!success)
            {
                return new NormalizationResult 
                { 
                    Success = false, 
                    Error = "Second pass normalization failed" 
                };
            }

            // Get output file info
            var outputInfo = new FileInfo(outputPath);
            
            return new NormalizationResult
            {
                Success = true,
                OutputPath = outputPath,
                OutputSizeBytes = outputInfo.Exists ? outputInfo.Length : 0,
                MeasuredIntegratedLoudness = measuredValues.MeasuredI,
                MeasuredTruePeak = measuredValues.MeasuredTP,
                MeasuredLoudnessRange = measuredValues.MeasuredLRA
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Normalization failed for: {Input}", inputPath);
            return new NormalizationResult 
            { 
                Success = false, 
                Error = ex.Message 
            };
        }
    }

    private async Task<MeasuredValues?> RunFirstPassAsync(string inputPath, NormalizationOptions options, CancellationToken ct)
    {
        var arguments = $"-i \"{inputPath}\" -af loudnorm=I={options.TargetLoudness}:TP={options.TruePeak}:LRA={options.LoudnessRange}:print_format=json -f null -";
        
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync(ct);
        var error = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        // Parse JSON from stderr (ffmpeg outputs loudnorm stats to stderr)
        var jsonMatch = System.Text.RegularExpressions.Regex.Match(error, @"\{[^}]*\"input_i\"[^}]*\}");
        if (jsonMatch.Success)
        {
            try
            {
                var json = jsonMatch.Value;
                var values = System.Text.Json.JsonSerializer.Deserialize<MeasuredValues>(json);
                return values;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse loudnorm output");
            }
        }

        return null;
    }

    private async Task<bool> RunSecondPassAsync(string inputPath, string outputPath, NormalizationOptions options, MeasuredValues measured, CancellationToken ct)
    {
        var arguments = $"-i \"{inputPath}\" -af loudnorm=I={options.TargetLoudness}:TP={options.TruePeak}:LRA={options.LoudnessRange}:measured_I={measured.MeasuredI}:measured_TP={measured.MeasuredTP}:measured_LRA={measured.MeasuredLRA}:measured_thresh={measured.MeasuredThresh}:offset={measured.Offset} -ar 44100 \"{outputPath}\"";
        
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        process.Start();
        await process.StandardOutput.ReadToEndAsync(ct);
        var error = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
        {
            _logger.LogError("ffmpeg normalization failed: {Error}", error);
            return false;
        }

        return File.Exists(outputPath);
    }
}

public class NormalizationOptions
{
    // Target loudness in LUFS (EBU R128 standard: -23, streaming services: -14 to -16)
    public double TargetLoudness { get; set; } = -14;  // Default: -14 LUFS (YouTube/Spotify standard)
    
    // True peak limit in dBTP (-1 to -3 dB)
    public double TruePeak { get; set; } = -1;
    
    // Loudness range in LU (1-20, higher = more dynamic range)
    public double LoudnessRange { get; set; } = 11;
}

public class NormalizationResult
{
    public bool Success { get; set; }
    public string? OutputPath { get; set; }
    public long OutputSizeBytes { get; set; }
    public double? MeasuredIntegratedLoudness { get; set; }
    public double? MeasuredTruePeak { get; set; }
    public double? MeasuredLoudnessRange { get; set; }
    public string? Error { get; set; }
}

public class MeasuredValues
{
    public double MeasuredI { get; set; }
    public double MeasuredTP { get; set; }
    public double MeasuredLRA { get; set; }
    public double MeasuredThresh { get; set; }
    public double Offset { get; set; }
}
