using DownloadApi;
using DownloadApi.Data;
using DownloadApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddSingleton<IAudioExtractor, YtDlpExtractor>();
builder.Services.AddSingleton<AudioNormalizer>();
builder.Services.AddHealthChecks();

// Add memory cache for YouTube API responses
builder.Services.AddMemoryCache();

// Add HTTP client for YouTube API
builder.Services.AddHttpClient<IYouTubeSearchService, YouTubeSearchService>();

// Add HTTP client for YouTube Playlist API
builder.Services.AddHttpClient<IPlaylistService, YouTubePlaylistService>();

// Add HTTP client for MusicBrainz API
builder.Services.AddHttpClient<IMusicBrainzService, MusicBrainzService>();

// Add HTTP client for Radio API (SRG SSR)
builder.Services.AddHttpClient<IRadioService, SrgSsrRadioService>();

// Add email service
builder.Services.AddTransient<IEmailService, SmtpEmailService>();

// Add quota service
builder.Services.AddScoped<IQuotaService, QuotaService>();

// Add SQLite repository
builder.Services.AddSingleton<JobRepository>(sp => 
{
    var logger = sp.GetRequiredService<ILogger<JobRepository>>();
    var dbPath = "/data/jobs.db";
    Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
    return new JobRepository(dbPath, logger);
});

var app = builder.Build();
var _logger = app.Services.GetRequiredService<ILogger<Program>>();

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

// ========== YouTube Search (Real API) ==========
app.MapGet("/api/search/youtube", async (string? q, IYouTubeSearchService youTubeService) =>
{
    if (string.IsNullOrWhiteSpace(q))
    {
        return Results.BadRequest(new { Error = "Query parameter 'q' is required" });
    }

    try
    {
        var result = await youTubeService.SearchAsync(q, maxResults: 10);
        return Results.Ok(new
        {
            Query = result.Query,
            Results = result.Results.Select(r => new
            {
                r.VideoId,
                r.Title,
                r.Author,
                r.Duration,
                r.ThumbnailUrl
            }),
            result.TotalResults,
            Cached = false
        });
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("API key"))
    {
        _logger.LogError(ex, "YouTube API configuration error");
        return Results.Problem(
            title: "YouTube API Error",
            detail: "YouTube API key not configured or invalid. Please check your configuration.",
            statusCode: 500);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to search YouTube");
        return Results.Problem(
            title: "Search Error",
            detail: "Failed to search YouTube. Please try again later.",
            statusCode: 500);
    }
});

// ========== MusicBrainz Search (Real API) ==========
app.MapGet("/api/search/musicbrainz", async (string? type, string? q, IMusicBrainzService musicBrainzService) =>
{
    if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(q))
    {
        return Results.BadRequest(new { Error = "Parameters 'type' and 'q' are required" });
    }

    if (!new[] { "artist", "album", "track" }.Contains(type.ToLower()))
    {
        return Results.BadRequest(new { Error = "Type must be 'artist', 'album', or 'track'" });
    }

    try
    {
        var result = type.ToLower() switch
        {
            "artist" => await musicBrainzService.SearchArtistsAsync(q, limit: 10),
            "track" => await musicBrainzService.SearchTracksAsync(q, limit: 10),
            "album" => await musicBrainzService.SearchReleasesAsync(q, limit: 10),
            _ => throw new InvalidOperationException("Invalid search type")
        };

        return Results.Ok(new
        {
            result.Query,
            result.Type,
            Artists = result.Artists.Select(a => new
            {
                a.Id,
                a.Name,
                a.SortName,
                a.Country,
                a.Type,
                a.Disambiguation,
                a.Score
            }),
            Tracks = result.Tracks.Select(t => new
            {
                t.Id,
                t.Title,
                t.FormattedDuration,
                t.Score,
                Artist = t.ArtistCredit.FirstOrDefault()?.Name,
                ArtistId = t.ArtistCredit.FirstOrDefault()?.Artist?.Id
            }),
            Releases = result.Releases.Select(r => new
            {
                r.Id,
                r.Title,
                r.Date,
                r.Country,
                r.TrackCount,
                r.Score,
                Artist = r.ArtistCredit.FirstOrDefault()?.Name
            }),
            result.TotalCount
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to search MusicBrainz");
        return Results.Problem(
            title: "Search Error",
            detail: "Failed to search MusicBrainz. Please try again later.",
            statusCode: 500);
    }
});

// ========== MusicBrainz Artist Details ==========
app.MapGet("/api/musicbrainz/artist/{artistId}", async (string artistId, IMusicBrainzService musicBrainzService) =>
{
    try
    {
        var artist = await musicBrainzService.GetArtistDetailsAsync(artistId);
        if (artist == null)
        {
            return Results.NotFound(new { Error = "Artist not found" });
        }

        return Results.Ok(new
        {
            artist.Id,
            artist.Name,
            artist.SortName,
            artist.Country,
            artist.Type,
            artist.Disambiguation,
            artist.Genres,
            artist.Urls
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to get MusicBrainz artist details");
        return Results.Problem(
            title: "Artist Details Error",
            detail: "Failed to retrieve artist details. Please try again later.",
            statusCode: 500);
    }
});

// ========== YouTube Playlist ==========

// Get playlist info
app.MapGet("/api/playlist/info", async (string url, IPlaylistService playlistService) =>
{
    if (string.IsNullOrWhiteSpace(url))
    {
        return Results.BadRequest(new { Error = "Playlist URL is required" });
    }

    try
    {
        var playlist = await playlistService.GetPlaylistInfoAsync(url);
        return Results.Ok(new
        {
            playlist.Id,
            playlist.Title,
            playlist.Description,
            playlist.Author,
            playlist.ThumbnailUrl,
            playlist.VideoCount,
            playlist.PublishedAt
        });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to get playlist info");
        return Results.Problem(
            title: "Playlist Error",
            detail: "Failed to retrieve playlist information.",
            statusCode: 500);
    }
});

// Get playlist videos
app.MapGet("/api/playlist/videos", async (string playlistId, IPlaylistService playlistService) =>
{
    if (string.IsNullOrWhiteSpace(playlistId))
    {
        return Results.BadRequest(new { Error = "Playlist ID is required" });
    }

    try
    {
        var videos = await playlistService.GetPlaylistVideosAsync(playlistId);
        return Results.Ok(new
        {
            PlaylistId = playlistId,
            TotalVideos = videos.Count,
            Videos = videos.Select(v => new
            {
                v.Position,
                v.VideoId,
                v.Title,
                v.Author,
                v.ThumbnailUrl,
                v.PublishedAt
            })
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to get playlist videos");
        return Results.Problem(
            title: "Playlist Error",
            detail: "Failed to retrieve playlist videos.",
            statusCode: 500);
    }
});

// Start playlist download
app.MapPost("/api/playlist/download", async (PlaylistDownloadRequest request, JobRepository repo, IAudioExtractor extractor, IQuotaService quotaService, IEmailService emailService) =>
{
    if (string.IsNullOrWhiteSpace(request.PlaylistId) || string.IsNullOrWhiteSpace(request.UserId))
    {
        return Results.BadRequest(new { Error = "PlaylistId and UserId are required" });
    }

    // Get playlist videos
    var playlistService = app.Services.GetRequiredService<IPlaylistService>();
    var videos = await playlistService.GetPlaylistVideosAsync(request.PlaylistId);

    if (!videos.Any())
    {
        return Results.BadRequest(new { Error = "No videos found in playlist" });
    }

    // Filter selected videos if specified
    var videosToDownload = request.SelectedVideoIds?.Any() == true
        ? videos.Where(v => request.SelectedVideoIds.Contains(v.VideoId)).ToList()
        : videos;

    // Check quota for all videos (estimate 10MB per video)
    var estimatedTotalBytes = videosToDownload.Count * 10L * 1024 * 1024; // 10MB estimate
    var wouldExceed = await quotaService.WouldExceedQuotaAsync(request.UserId, estimatedTotalBytes);
    
    if (wouldExceed)
    {
        var quota = await quotaService.GetUserQuotaAsync(request.UserId);
        return Results.Problem(
            title: "Storage Quota Warning",
            detail = $"This playlist download may exceed your storage quota. You have {quota.RemainingMB:F0} MB remaining.",
            statusCode: 403);
    }

    // Create jobs for each video
    var jobs = new List<object>();
    foreach (var video in videosToDownload)
    {
        var job = await repo.CreateJobAsync(
            request.UserId,
            $"https://youtube.com/watch?v={video.VideoId}",
            request.Format?.ToLower() ?? "mp3",
            video.Title,
            video.Author
        );

        // Start async download
        _ = Task.Run(async () =>
        {
            await ProcessDownloadAsync(job.Id, repo, extractor, app.Services.GetRequiredService<AudioNormalizer>(), quotaService, emailService, request.Normalize, request.NormalizationLevel);
        });

        jobs.Add(new { job.Id, job.Title, job.Status });
    }

    return Results.Accepted("/api/jobs", new
    {
        Message = $"Started downloading {jobs.Count} videos from playlist",
        TotalVideos = videosToDownload.Count,
        Jobs = jobs
    });
});

// ========== Download Start ==========
app.MapPost("/api/download/start", async (DownloadRequest request, JobRepository repo, IAudioExtractor extractor, IQuotaService quotaService, IEmailService emailService) =>
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

    // Check quota before allowing download
    var quota = await quotaService.GetUserQuotaAsync(request.UserId);
    if (quota.Threshold == QuotaThreshold.Blocked)
    {
        return Results.Problem(
            title: "Storage Quota Exceeded",
            detail: $"Your storage is {quota.PercentageUsed:F0}% full. Please delete some files before downloading.",
            statusCode: 403);
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
        await ProcessDownloadAsync(job.Id, repo, extractor, builder.Services.BuildServiceProvider().GetRequiredService<AudioNormalizer>(), quotaService, emailService, request.Normalize, request.NormalizationLevel);
    });

    return Results.Accepted($"/api/download/status/{job.Id}", new
    {
        JobId = job.Id,
        Status = job.Status.ToString(),
        QuotaPercent = quota.PercentageUsed,
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
        job.VideoId,
        job.Title,
        job.Author,
        job.Format,
        Status = job.Status.ToString(),
        job.Progress,
        job.OriginalFilename,
        job.CorrectedFilename,
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

// ========== File Management ==========

// List user's downloaded files
app.MapGet("/api/files/{userId}", async (string userId, JobRepository repo) =>
{
    var jobs = await repo.GetUserJobsAsync(userId);
    var files = jobs
        .Where(j => j.Status == JobStatus.Completed && !string.IsNullOrEmpty(j.FilePath))
        .Select(j => new
        {
            j.Id,
            j.Title,
            j.Author,
            j.Format,
            FileSizeBytes = j.FileSizeBytes,
            FileSizeMB = Math.Round(j.FileSizeBytes / (1024.0 * 1024.0), 2),
            j.CreatedAt,
            j.FilePath
        });
    
    return Results.Ok(files);
});

// Download a file
app.MapGet("/api/files/{userId}/download/{jobId}", async (string userId, string jobId, JobRepository repo) =>
{
    var job = await repo.GetJobAsync(jobId);
    
    if (job == null)
        return Results.NotFound(new { Error = "Job not found" });
    
    if (job.UserId != userId)
        return Results.Forbid();
    
    if (job.Status != JobStatus.Completed || string.IsNullOrEmpty(job.FilePath))
        return Results.BadRequest(new { Error = "File not available for download" });
    
    if (!File.Exists(job.FilePath))
        return Results.NotFound(new { Error = "File not found on disk" });
    
    var fileName = Path.GetFileName(job.FilePath);
    var mimeType = job.Format?.ToLower() switch
    {
        "mp3" => "audio/mpeg",
        "flac" => "audio/flac",
        "m4a" => "audio/mp4",
        _ => "application/octet-stream"
    };
    
    return Results.File(job.FilePath, mimeType, fileName);
});

// ========== Admin Statistics ==========

// Get global stats (admin only)
app.MapGet("/api/admin/stats/global", async (JobRepository repo) =>
{
    var stats = await repo.GetGlobalStatsAsync();
    return Results.Ok(new
    {
        stats.TotalDownloads,
        stats.TotalStorageBytes,
        TotalStorageMB = Math.Round(stats.TotalStorageMB, 2),
        stats.DownloadsPerDay,
        stats.StatusCounts,
        stats.ActiveUsersLast7Days
    });
});

// Get all user stats (admin only)
app.MapGet("/api/admin/stats/users", async (JobRepository repo) =>
{
    var users = await repo.GetUserStatsAsync();
    return Results.Ok(users.Select(u => new
    {
        u.UserId,
        u.TotalDownloads,
        TotalStorageMB = Math.Round(u.TotalStorageMB, 2),
        u.CompletedDownloads,
        u.FailedDownloads,
        u.LastActive
    }));
});

// ========== Admin Whitelist Management ==========

// Get all whitelisted users (admin only)
app.MapGet("/api/admin/whitelist", async (JobRepository repo) =>
{
    var users = await repo.GetWhitelistAsync();
    return Results.Ok(users);
});

// Add user to whitelist (admin only)
app.MapPost("/api/admin/whitelist", async (AddToWhitelistRequest request, JobRepository repo, IEmailService emailService, HttpContext httpContext) =>
{
    // Check if already whitelisted
    var existing = await repo.GetWhitelistByUserIdAsync(request.UserId);
    if (existing != null)
    {
        return Results.Conflict(new { Message = "User is already whitelisted" });
    }

    var adminId = httpContext.User.Identity?.Name ?? "system";
    var entry = await repo.AddToWhitelistAsync(request.UserId, adminId, request.SendWelcomeEmail);

    _logger.LogInformation("Added {UserId} to whitelist by {AdminId}", request.UserId, adminId);

    // Send welcome email if requested
    if (request.SendWelcomeEmail)
    {
        try
        {
            await emailService.SendWelcomeEmailAsync(request.UserId, request.UserId);
            _logger.LogInformation("Welcome email sent to {Email}", request.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email}", request.UserId);
            // Don't fail the request if email fails, just log it
        }
    }

    return Results.Created($"/api/admin/whitelist/{entry.Id}", entry);
});

// Update whitelist status (admin only)
app.MapPut("/api/admin/whitelist/{id}", async (string id, UpdateWhitelistRequest request, JobRepository repo) =>
{
    var entry = await repo.GetWhitelistEntryAsync(id);
    if (entry == null) return Results.NotFound(new { Error = "Entry not found" });

    await repo.UpdateWhitelistStatusAsync(id, request.IsActive);

    _logger.LogInformation("Updated whitelist status for {UserId} to {IsActive}", entry.UserId, request.IsActive);

    return Results.NoContent();
});

// ========== User Profile ==========

// Get user profile
app.MapGet("/api/user/profile/{userId}", async (string userId, JobRepository repo) =>
{
    var stats = await repo.GetUserDetailStatsAsync(userId);
    if (stats == null)
        return Results.NotFound(new { Error = "User not found" });

    return Results.Ok(new
    {
        stats.UserId,
        TotalDownloads = stats.TotalDownloads,
        TotalStorageMB = Math.Round(stats.TotalStorageMB, 2),
        stats.CompletedDownloads,
        stats.FailedDownloads,
        stats.LastActive,
        stats.TopArtists,
        stats.DownloadsPerDay
    });
});

// Get user settings
app.MapGet("/api/user/settings/{userId}", (string userId) =>
{
    // Return default settings (would be stored in DB in production)
    return Results.Ok(new
    {
        DefaultFormat = "mp3",
        EnableNormalization = false,
        NormalizationLevel = -14,
        EmailNotifications = true
    });
});

// Update user settings
app.MapPut("/api/user/settings/{userId}", (string userId, UserSettingsRequest request) =>
{
    // Update settings (would be stored in DB in production)
    return Results.Ok(new
    {
        Message = "Settings updated successfully",
        Settings = request
    });
});

// Remove from whitelist (admin only)
app.MapDelete("/api/admin/whitelist/{id}", async (string id, JobRepository repo) =>
{
    var entry = await repo.GetWhitelistEntryAsync(id);
    if (entry == null) return Results.NotFound(new { Error = "Entry not found" });

    await repo.RemoveFromWhitelistAsync(id);
    
    _logger.LogInformation("Removed {UserId} from whitelist", entry.UserId);
    
    return Results.NoContent();
});

// ========== Quota Management ==========

// Get user quota info
app.MapGet("/api/quota/{userId}", async (string userId, IQuotaService quotaService) =>
{
    var quota = await quotaService.GetUserQuotaAsync(userId);
    return Results.Ok(new
    {
        quota.UserId,
        quota.TotalBytesAllowed,
        quota.TotalBytesUsed,
        quota.RemainingBytes,
        quota.PercentageUsed,
        quota.FileCount,
        quota.Threshold,
        UsedMB = quota.UsedMB,
        TotalMB = quota.TotalMB,
        RemainingMB = quota.RemainingMB
    });
});

// Check if download would exceed quota
app.MapGet("/api/quota/{userId}/check", async (string userId, long fileSizeBytes, IQuotaService quotaService) =>
{
    var wouldExceed = await quotaService.WouldExceedQuotaAsync(userId, fileSizeBytes);
    var quota = await quotaService.GetUserQuotaAsync(userId);
    
    return Results.Ok(new
    {
        WouldExceed = wouldExceed,
        CurrentUsage = quota.PercentageUsed,
        RemainingMB = quota.RemainingMB,
        Threshold = quota.Threshold
    });
});

// Get specific user stats (admin only)
app.MapGet("/api/admin/stats/users/{userId}", async (string userId, JobRepository repo) =>
{
    var stats = await repo.GetUserDetailStatsAsync(userId);
    if (stats == null)
        return Results.NotFound(new { Error = "User not found" });
    
    return Results.Ok(new
    {
        stats.UserId,
        stats.TotalDownloads,
        TotalStorageMB = Math.Round(stats.TotalStorageMB, 2),
        stats.CompletedDownloads,
        stats.FailedDownloads,
        stats.LastActive,
        stats.TopArtists,
        stats.DownloadsPerDay
    });
});

// Delete a file
app.MapDelete("/api/files/{userId}/{jobId}", async (string userId, string jobId, JobRepository repo) =>
{
    var job = await repo.GetJobAsync(jobId);
    
    if (job == null)
        return Results.NotFound(new { Error = "Job not found" });
    
    if (job.UserId != userId)
        return Results.Forbid();
    
    // Delete file from disk if exists
    if (!string.IsNullOrEmpty(job.FilePath) && File.Exists(job.FilePath))
    {
        try
        {
            File.Delete(job.FilePath);
            
            // Try to remove empty parent directories
            var dir = Path.GetDirectoryName(job.FilePath);
            if (dir != null && Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
            {
                Directory.Delete(dir);
                
                // Try to remove user directory if empty
                var userDir = Path.GetDirectoryName(dir);
                if (userDir != null && Directory.Exists(userDir) && !Directory.EnumerateFileSystemEntries(userDir).Any())
                {
                    Directory.Delete(userDir);
                }
            }
        }
        catch (Exception ex)
        {
            // Log but don't fail - DB record will still be deleted
            Console.WriteLine($"Warning: Could not delete file {job.FilePath}: {ex.Message}");
        }
    }
    
    // Delete from database
    await repo.DeleteJobAsync(jobId);
    
    return Results.Ok(new { Message = "File deleted successfully" });
});

// ========== Radio Now Playing ==========

// Get available radio stations
app.MapGet("/api/radio/stations", async (IRadioService radioService) =>
{
    try
    {
        var stations = await radioService.GetStationsAsync();
        return Results.Ok(new
        {
            Stations = stations.Select(s => new
            {
                s.Id,
                s.Name,
                s.Provider
            })
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to get radio stations");
        return Results.Problem("Failed to retrieve radio stations", statusCode: 500);
    }
});

// Get current playlist for a station
app.MapGet("/api/radio/playlist", async (string station, int? limit, IRadioService radioService) =>
{
    if (string.IsNullOrWhiteSpace(station))
    {
        return Results.BadRequest(new { Error = "Station parameter is required" });
    }

    try
    {
        var playlist = await radioService.GetPlaylistAsync(station, limit ?? 20);
        var nowPlaying = playlist.FirstOrDefault(s => s.IsPlayingNow);
        
        return Results.Ok(new
        {
            Station = station,
            NowPlaying = nowPlaying != null ? new
            {
                nowPlaying.Artist,
                nowPlaying.Title,
                nowPlaying.FormattedDuration,
                nowPlaying.IsPlayingNow
            } : null,
            Songs = playlist.Select(s => new
            {
                s.Artist,
                s.Title,
                s.PlayedAt,
                s.FormattedDuration,
                s.IsPlayingNow,
                s.SearchQuery
            })
        });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to get radio playlist for {Station}", station);
        return Results.Problem("Failed to retrieve playlist", statusCode: 500);
    }
});

// Get currently playing song
app.MapGet("/api/radio/now-playing", async (string station, IRadioService radioService) =>
{
    if (string.IsNullOrWhiteSpace(station))
    {
        return Results.BadRequest(new { Error = "Station parameter is required" });
    }

    try
    {
        var song = await radioService.GetNowPlayingAsync(station);
        if (song == null)
        {
            return Results.NotFound(new { Error = "No song currently playing or station not found" });
        }

        return Results.Ok(new
        {
            song.Artist,
            song.Title,
            song.PlayedAt,
            song.FormattedDuration,
            song.IsPlayingNow,
            song.Station,
            song.StationName,
            song.SearchQuery
        });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to get now playing for {Station}", station);
        return Results.Problem("Failed to retrieve now playing", statusCode: 500);
    }
});

// Download currently playing song (Agent API)
app.MapPost("/api/radio/download-current", async (
    RadioDownloadRequest request,
    IRadioService radioService,
    IYouTubeSearchService youTubeService,
    IQuotaService quotaService,
    JobRepository repo,
    IAudioExtractor extractor) =>
{
    if (string.IsNullOrWhiteSpace(request.Station))
    {
        return Results.BadRequest(new { Error = "Station is required" });
    }

    try
    {
        // Get currently playing song
        var song = await radioService.GetNowPlayingAsync(request.Station);
        if (song == null)
        {
            return Results.NotFound(new { Error = $"No song currently playing on {request.Station}" });
        }

        // Check quota
        var userId = request.UserId ?? "agent-user";
        var quota = await quotaService.GetUserQuotaAsync(userId);
        if (quota.Threshold == QuotaThreshold.Blocked)
        {
            return Results.Problem(
                title: "Storage Quota Exceeded",
                detail: $"Storage is {quota.PercentageUsed:F0}% full. Please free up space.",
                statusCode: 429);
        }

        // Search YouTube
        var searchResult = await youTubeService.SearchAsync(song.SearchQuery, 5);
        if (searchResult?.Results?.Any() != true)
        {
            return Results.NotFound(new { Error = "No YouTube results found for this song" });
        }

        // Select best match
        var bestMatch = request.AutoSelectBestMatch 
            ? searchResult.Results.First() 
            : null;

        if (bestMatch == null)
        {
            return Results.Ok(new
            {
                Song = new { song.Artist, song.Title, song.Station, song.PlayedAt },
                YouTubeResults = searchResult.Results.Select(r => new
                {
                    r.VideoId,
                    r.Title,
                    r.Author,
                    r.Duration,
                    r.ThumbnailUrl
                }),
                Message = "Multiple matches found. Please select one."
            });
        }

        // Create download job
        var format = request.Format?.ToLower() ?? "mp3";
        var job = await repo.CreateJobAsync(
            userId,
            $"https://youtube.com/watch?v={bestMatch.VideoId}",
            format,
            $"{song.Artist} - {song.Title}",
            song.Artist
        );

        // Start download
        _ = Task.Run(async () =>
        {
            await ProcessDownloadAsync(job.Id, repo, extractor, 
                app.Services.GetRequiredService<AudioNormalizer>(),
                quotaService,
                app.Services.GetRequiredService<IEmailService>(),
                request.Normalize, 
                request.NormalizationLevel);
        });

        return Results.Accepted("/api/download/status/" + job.Id, new
        {
            Success = true,
            Song = new { song.Artist, song.Title, song.Station, song.PlayedAt },
            Download = new
            {
                job.Id,
                Status = "queued",
                YouTubeUrl = $"https://youtube.com/watch?v={bestMatch.VideoId}",
                YouTubeTitle = bestMatch.Title
            }
        });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to download current song from {Station}", request.Station);
        return Results.Problem("Failed to process download request", statusCode: 500);
    }
});

// Background download processor with retry logic
async Task ProcessDownloadAsync(string jobId, JobRepository repo, IAudioExtractor extractor, AudioNormalizer normalizer, IQuotaService quotaService, IEmailService emailService, bool normalize, double? normalizationLevel)
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
                // Store filenames before updating status
                await repo.UpdateFilenamesAsync(jobId, result.OriginalFilename, result.CorrectedFilename);
                
                string finalFilePath = result.FilePath;
                long finalFileSize = result.FileSizeBytes;

                // Apply audio normalization if requested
                if (normalize)
                {
                    await repo.UpdateJobStatusAsync(jobId, JobStatus.Downloading, 80, errorMessage: "Normalizing audio...");
                    
                    var normOptions = new NormalizationOptions();
                    if (normalizationLevel.HasValue)
                    {
                        normOptions.TargetLoudness = normalizationLevel.Value;
                    }
                    
                    var normOutputPath = result.FilePath + ".normalized.mp3";
                    var normResult = await normalizer.NormalizeAsync(result.FilePath, normOutputPath, normOptions);
                    
                    if (normResult.Success)
                    {
                        // Replace original with normalized
                        File.Delete(result.FilePath);
                        File.Move(normOutputPath, result.FilePath);
                        finalFileSize = normResult.OutputSizeBytes;
                    }
                    else
                    {
                        _logger.LogWarning("Normalization failed for {JobId}: {Error}. Using unnormalized file.", jobId, normResult.Error);
                        // Clean up failed normalization file if exists
                        if (File.Exists(normOutputPath))
                        {
                            File.Delete(normOutputPath);
                        }
                    }
                }
                
                await repo.UpdateJobStatusAsync(
                    jobId, 
                    JobStatus.Completed, 
                    100, 
                    finalFilePath, 
                    finalFileSize
                );
                
                // Check quota after successful download and send notification if needed
                try
                {
                    await quotaService.CheckAndNotifyQuotaAsync(job.UserId);
                }
                catch (Exception quotaEx)
                {
                    _logger.LogError(quotaEx, "Failed to check quota after download for user {UserId}", job.UserId);
                }
                
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
public record AddToWhitelistRequest(string UserId, bool SendWelcomeEmail = false);
public record UpdateWhitelistRequest(bool IsActive);

public record DownloadRequest(
    string Url,
    string UserId,
    string? Format = "mp3",
    string? Title = null,
    string? Author = null,
    bool Normalize = false,
    double? NormalizationLevel = null
);

public record PlaylistDownloadRequest(
    string PlaylistId,
    string UserId,
    List<string>? SelectedVideoIds,
    string? Format,
    bool Normalize,
    double? NormalizationLevel
);

public record UserSettingsRequest(
    string DefaultFormat,
    bool EnableNormalization,
    int NormalizationLevel,
    bool EmailNotifications
);

public record RadioDownloadRequest(
    string Station,
    string? UserId,
    string? Format,
    bool AutoSelectBestMatch,
    bool Normalize,
    double? NormalizationLevel
);
