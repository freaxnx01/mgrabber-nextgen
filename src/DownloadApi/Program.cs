using DownloadApi;
using DownloadApi.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddSingleton<IAudioExtractor, YtDlpExtractor>();
builder.Services.AddHealthChecks();

// Add SQLite repository
builder.Services.AddSingleton<JobRepository>(sp => 
{
    var logger = sp.GetRequiredService<ILogger<JobRepository>>();
    var dbPath = "/data/jobs.db";
    Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
    return new JobRepository(dbPath, logger);
});

var app = builder.Build();

// Health check endpoint
app.MapGet("/api/health", async (IAudioExtractor extractor) =>
{
    var version = await extractor.GetVersionAsync();
    var ffmpegAvailable = File.Exists("/usr/bin/ffmpeg");
    
    return Results.Ok(new
    {
        Status = "Healthy",
        Timestamp = DateTime.UtcNow,
        Services = new
        {
            YtDlp = version,
            Ffmpeg = ffmpegAvailable ? "Available" : "Not Found"
        }
    });
});

// ========== YouTube Search (Mock) ==========
app.MapGet("/api/search/youtube", (string? q) =>
{
    if (string.IsNullOrWhiteSpace(q))
    {
        return Results.BadRequest(new { Error = "Query parameter 'q' is required" });
    }

    // Mock data for MVP
    var mockResults = new[]
    {
        new {
            VideoId = "dQw4w9WgXcQ",
            Title = $"{q} - Official Audio",
            Author = "Music Channel",
            Duration = "3:45",
            ThumbnailUrl = "https://i.ytimg.com/vi/dQw4w9WgXcQ/hqdefault.jpg"
        },
        new {
            VideoId = "abc123xyz",
            Title = $"{q} - Live Performance",
            Author = "Live Music",
            Duration = "4:20",
            ThumbnailUrl = "https://i.ytimg.com/vi/abc123xyz/hqdefault.jpg"
        }
    };

    return Results.Ok(new
    {
        Query = q,
        Results = mockResults,
        TotalResults = mockResults.Length,
        Note = "Mock data for MVP - integrate YouTube Data API v3 for real results"
    });
});

// ========== MusicBrainz Search (Mock) ==========
app.MapGet("/api/search/musicbrainz", (string? type, string? q) =>
{
    if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(q))
    {
        return Results.BadRequest(new { Error = "Parameters 'type' and 'q' are required" });
    }

    if (!new[] { "artist", "album", "track" }.Contains(type.ToLower()))
    {
        return Results.BadRequest(new { Error = "Type must be 'artist', 'album', or 'track'" });
    }

    // Mock data
    var mockResults = type.ToLower() switch
    {
        "artist" => new[]
        {
            new { Id = "artist-1", Name = q, Type = "Group", Country = "US" },
            new { Id = "artist-2", Name = $"{q} (Band)", Type = "Group", Country = "UK" }
        },
        "album" => new[]
        {
            new { Id = "album-1", Title = $"{q} - Greatest Hits", Artist = "Various Artists", Year = 2024 },
            new { Id = "album-2", Title = $"{q} - Live", Artist = q, Year = 2023 }
        },
        "track" => new[]
        {
            new { Id = "track-1", Title = $"{q} Song", Artist = "Test Artist", Duration = "3:30" },
            new { Id = "track-2", Title = $"Best of {q}", Artist = "Various", Duration = "4:15" }
        },
        _ => Array.Empty<object>()
    };

    return Results.Ok(new
    {
        Query = q,
        Type = type,
        Results = mockResults,
        TotalResults = mockResults.Length,
        Note = "Mock data for MVP - integrate real MusicBrainz API for production"
    });
});

// ========== Download Start ==========
app.MapPost("/api/download/start", async (DownloadRequest request, JobRepository repo, IAudioExtractor extractor) =>
{
    if (string.IsNullOrWhiteSpace(request.Url) || string.IsNullOrWhiteSpace(request.UserId))
    {
        return Results.BadRequest(new { Error = "Url and UserId are required" });
    }

    // Validate format (MP3 only for MVP)
    var format = request.Format?.ToLower() ?? "mp3";
    if (format != "mp3")
    {
        return Results.BadRequest(new { Error = "Only MP3 format is supported in MVP" });
    }

    // Create job in database
    var job = await repo.CreateJobAsync(
        request.UserId,
        request.Url,
        format,
        request.Title,
        request.Author
    );

    // Start async extraction (fire and forget for MVP)
    _ = Task.Run(async () =>
    {
        await ProcessDownloadAsync(job.Id, repo, extractor);
    });

    return Results.Accepted($"/api/download/status/{job.Id}", new
    {
        JobId = job.Id,
        Status = job.Status.ToString(),
        Message = "Download started"
    });
});

// ========== Download Status ==========
app.MapGet("/api/download/status/{jobId}", async (string jobId, JobRepository repo) =>
{
    var job = await repo.GetJobAsync(jobId);
    if (job == null)
    {
        return Results.NotFound(new { Error = "Job not found" });
    }

    return Results.Ok(new
    {
        job.Id,
        job.UserId,
        job.Url,
        job.Title,
        job.Author,
        job.Format,
        Status = job.Status.ToString(),
        job.Progress,
        job.FilePath,
        job.FileSizeBytes,
        job.ErrorMessage,
        job.RetryCount,
        job.CreatedAt,
        job.CompletedAt,
        job.UpdatedAt
    });
});

// ========== List User Jobs ==========
app.MapGet("/api/jobs/{userId}", async (string userId, JobRepository repo) =>
{
    var jobs = await repo.GetUserJobsAsync(userId);
    return Results.Ok(jobs.Select(j => new
    {
        j.Id,
        j.Url,
        j.Title,
        j.Author,
        Status = j.Status.ToString(),
        j.Progress,
        j.FileSizeBytes,
        j.CreatedAt
    }));
});

// Background download processor with retry logic
async Task ProcessDownloadAsync(string jobId, JobRepository repo, IAudioExtractor extractor)
{
    const int maxRetries = 3;
    
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            var job = await repo.GetJobAsync(jobId);
            if (job == null) return;

            // Update status to downloading
            await repo.UpdateJobStatusAsync(jobId, JobStatus.Downloading, 10);

            // Perform extraction with custom filename
            var result = await extractor.ExtractAsync(
                job.Url, 
                AudioFormat.Mp3, 
                job.UserId,
                job.Id,
                job.Author,
                job.Title,
                CancellationToken.None);

            if (result.Success && !string.IsNullOrEmpty(result.FilePath))
            {
                await repo.UpdateJobStatusAsync(
                    jobId, 
                    JobStatus.Completed, 
                    100, 
                    result.FilePath, 
                    result.FileSizeBytes
                );
                return; // Success!
            }
            else
            {
                throw new Exception(result.Error ?? "Extraction failed");
            }
        }
        catch (Exception ex)
        {
            await repo.IncrementRetryCountAsync(jobId);
            
            if (attempt >= maxRetries)
            {
                await repo.UpdateJobStatusAsync(jobId, JobStatus.Failed, errorMessage: ex.Message);
            }
            else
            {
                // Wait before retry (exponential backoff)
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
            }
        }
    }
}

app.Run();

// DTOs
public record DownloadRequest(
    string Url,
    string UserId,
    string? Format = "mp3",
    string? Title = null,
    string? Author = null
);
