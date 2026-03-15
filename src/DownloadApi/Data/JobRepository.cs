using Microsoft.Data.Sqlite;

namespace DownloadApi.Data;

public class JobRepository
{
    private readonly string _connectionString;
    private readonly ILogger<JobRepository> _logger;

    public JobRepository(string dbPath, ILogger<JobRepository> logger)
    {
        _connectionString = $"Data Source={dbPath}";
        _logger = logger;
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS DownloadJobs (
                Id TEXT PRIMARY KEY,
                UserId TEXT NOT NULL,
                Url TEXT NOT NULL,
                VideoId TEXT,
                Title TEXT,
                Author TEXT,
                Format TEXT NOT NULL,
                Status TEXT NOT NULL,
                Progress INTEGER DEFAULT 0,
                OriginalFilename TEXT,
                CorrectedFilename TEXT,
                FilePath TEXT,
                FileSizeBytes INTEGER DEFAULT 0,
                ErrorMessage TEXT,
                RetryCount INTEGER DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                CompletedAt TEXT,
                UpdatedAt TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_jobs_user ON DownloadJobs(UserId);
            CREATE INDEX IF NOT EXISTS idx_jobs_status ON DownloadJobs(Status);
            CREATE INDEX IF NOT EXISTS idx_jobs_videoid ON DownloadJobs(VideoId);

            CREATE TABLE IF NOT EXISTS Whitelist (
                Id TEXT PRIMARY KEY,
                UserId TEXT NOT NULL UNIQUE,
                AddedBy TEXT NOT NULL,
                AddedAt TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                WelcomeEmailSent INTEGER NOT NULL DEFAULT 0
            );

            CREATE INDEX IF NOT EXISTS idx_whitelist_user ON Whitelist(UserId);
        ";
        cmd.ExecuteNonQuery();

        // Migration: Add new columns if they don't exist (for existing databases)
        AddColumnIfNotExists(connection, "VideoId", "TEXT");
        AddColumnIfNotExists(connection, "OriginalFilename", "TEXT");
        AddColumnIfNotExists(connection, "CorrectedFilename", "TEXT");

        _logger.LogInformation("Database initialized");
    }

    private void AddColumnIfNotExists(SqliteConnection connection, string columnName, string columnType)
    {
        try
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = $"ALTER TABLE DownloadJobs ADD COLUMN {columnName} {columnType}";
            cmd.ExecuteNonQuery();
            _logger.LogInformation($"Added column {columnName}");
        }
        catch (SqliteException ex) when (ex.Message.Contains("duplicate column"))
        {
            // Column already exists, ignore
        }
    }

    public async Task<DownloadJob> CreateJobAsync(string userId, string url, string format, string? title = null, string? author = null)
    {
        var job = new DownloadJob
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = userId,
            Url = url,
            VideoId = ExtractVideoId(url),
            Title = title,
            Author = author,
            Format = format,
            Status = JobStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO DownloadJobs (Id, UserId, Url, VideoId, Title, Author, Format, Status, Progress, FileSizeBytes, RetryCount, CreatedAt, UpdatedAt)
            VALUES (@id, @userId, @url, @videoId, @title, @author, @format, @status, 0, 0, 0, @createdAt, @updatedAt)";
        
        cmd.Parameters.AddWithValue("@id", job.Id);
        cmd.Parameters.AddWithValue("@userId", job.UserId);
        cmd.Parameters.AddWithValue("@url", job.Url);
        cmd.Parameters.AddWithValue("@videoId", job.VideoId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@title", job.Title ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@author", job.Author ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@format", job.Format);
        cmd.Parameters.AddWithValue("@status", job.Status.ToString());
        cmd.Parameters.AddWithValue("@createdAt", job.CreatedAt.ToString("O"));
        cmd.Parameters.AddWithValue("@updatedAt", job.UpdatedAt.ToString("O"));

        await cmd.ExecuteNonQueryAsync();
        return job;
    }

    public async Task UpdateFilenamesAsync(string jobId, string? originalFilename, string? correctedFilename)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            UPDATE DownloadJobs 
            SET OriginalFilename = @originalFilename, CorrectedFilename = @correctedFilename, UpdatedAt = @updatedAt
            WHERE Id = @id";
        
        cmd.Parameters.AddWithValue("@id", jobId);
        cmd.Parameters.AddWithValue("@originalFilename", originalFilename ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@correctedFilename", correctedFilename ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("O"));

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<DownloadJob?> GetJobAsync(string jobId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM DownloadJobs WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", jobId);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapJob(reader);
        }
        return null;
    }

    public async Task<List<DownloadJob>> GetUserJobsAsync(string userId, int limit = 50)
    {
        var jobs = new List<DownloadJob>();
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM DownloadJobs WHERE UserId = @userId ORDER BY CreatedAt DESC LIMIT @limit";
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@limit", limit);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            jobs.Add(MapJob(reader));
        }
        return jobs;
    }

    public async Task UpdateJobStatusAsync(string jobId, JobStatus status, int? progress = null, string? filePath = null, long? fileSize = null, string? errorMessage = null)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        
        if (status == JobStatus.Completed)
        {
            cmd.CommandText = @"
                UPDATE DownloadJobs 
                SET Status = @status, Progress = @progress, FilePath = @filePath, FileSizeBytes = @fileSize, 
                    ErrorMessage = @errorMessage, CompletedAt = @completedAt, UpdatedAt = @updatedAt
                WHERE Id = @id";
            cmd.Parameters.AddWithValue("@completedAt", DateTime.UtcNow.ToString("O"));
        }
        else
        {
            cmd.CommandText = @"
                UPDATE DownloadJobs 
                SET Status = @status, Progress = @progress, FilePath = @filePath, FileSizeBytes = @fileSize, 
                    ErrorMessage = @errorMessage, UpdatedAt = @updatedAt
                WHERE Id = @id";
        }

        cmd.Parameters.AddWithValue("@id", jobId);
        cmd.Parameters.AddWithValue("@status", status.ToString());
        cmd.Parameters.AddWithValue("@progress", progress ?? 0);
        cmd.Parameters.AddWithValue("@filePath", filePath ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@fileSize", fileSize ?? 0);
        cmd.Parameters.AddWithValue("@errorMessage", errorMessage ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("O"));

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task IncrementRetryCountAsync(string jobId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE DownloadJobs SET RetryCount = RetryCount + 1, UpdatedAt = @updatedAt WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", jobId);
        cmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("O"));

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteJobAsync(string jobId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM DownloadJobs WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", jobId);

        await cmd.ExecuteNonQueryAsync();
    }

    // ========== Admin Statistics ==========

    public async Task<GlobalStats> GetGlobalStatsAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var stats = new GlobalStats();

        // Total downloads
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*), COALESCE(SUM(FileSizeBytes), 0) FROM DownloadJobs WHERE Status = 'Completed'";
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                stats.TotalDownloads = reader.GetInt32(0);
                stats.TotalStorageBytes = reader.GetInt64(1);
            }
        }

        // Downloads per day (last 30 days)
        cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT date(CreatedAt) as Day, COUNT(*) 
            FROM DownloadJobs 
            WHERE Status = 'Completed' AND CreatedAt > datetime('now', '-30 days')
            GROUP BY date(CreatedAt)
            ORDER BY Day";
        stats.DownloadsPerDay = new List<DailyDownloadCount>();
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                stats.DownloadsPerDay.Add(new DailyDownloadCount
                {
                    Date = reader.GetString(0),
                    Count = reader.GetInt32(1)
                });
            }
        }

        // Success/failure rate
        cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT Status, COUNT(*) 
            FROM DownloadJobs 
            GROUP BY Status";
        stats.StatusCounts = new Dictionary<string, int>();
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                stats.StatusCounts[reader.GetString(0)] = reader.GetInt32(1);
            }
        }

        // Active users (unique users in last 7 days)
        cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT COUNT(DISTINCT UserId) 
            FROM DownloadJobs 
            WHERE CreatedAt > datetime('now', '-7 days')";
        stats.ActiveUsersLast7Days = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        return stats;
    }

    public async Task<List<UserStats>> GetUserStatsAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT 
                UserId,
                COUNT(*) as TotalDownloads,
                COALESCE(SUM(FileSizeBytes), 0) as TotalStorage,
                COUNT(CASE WHEN Status = 'Completed' THEN 1 END) as CompletedDownloads,
                COUNT(CASE WHEN Status = 'Failed' THEN 1 END) as FailedDownloads,
                MAX(CreatedAt) as LastActive
            FROM DownloadJobs
            GROUP BY UserId
            ORDER BY TotalDownloads DESC";

        var userStats = new List<UserStats>();
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                userStats.Add(new UserStats
                {
                    UserId = reader.GetString(0),
                    TotalDownloads = reader.GetInt32(1),
                    TotalStorageBytes = reader.GetInt64(2),
                    CompletedDownloads = reader.GetInt32(3),
                    FailedDownloads = reader.GetInt32(4),
                    LastActive = reader.IsDBNull(5) ? null : DateTime.Parse(reader.GetString(5))
                });
            }
        }

        return userStats;
    }

    public async Task<UserDetailStats?> GetUserDetailStatsAsync(string userId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var stats = new UserDetailStats { UserId = userId };

        // Overall stats
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT 
                COUNT(*),
                COALESCE(SUM(FileSizeBytes), 0),
                COUNT(CASE WHEN Status = 'Completed' THEN 1 END),
                COUNT(CASE WHEN Status = 'Failed' THEN 1 END),
                MAX(CreatedAt)
            FROM DownloadJobs
            WHERE UserId = @userId";
        cmd.Parameters.AddWithValue("@userId", userId);

        using (var reader = await cmd.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                stats.TotalDownloads = reader.GetInt32(0);
                stats.TotalStorageBytes = reader.GetInt64(1);
                stats.CompletedDownloads = reader.GetInt32(2);
                stats.FailedDownloads = reader.GetInt32(3);
                stats.LastActive = reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4));
            }
        }

        if (stats.TotalDownloads == 0)
            return null;

        // Top artists
        cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT Author, COUNT(*) as Count
            FROM DownloadJobs
            WHERE UserId = @userId AND Author IS NOT NULL
            GROUP BY Author
            ORDER BY Count DESC
            LIMIT 10";
        cmd.Parameters.AddWithValue("@userId", userId);

        stats.TopArtists = new List<ArtistCount>();
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                stats.TopArtists.Add(new ArtistCount
                {
                    Artist = reader.GetString(0),
                    Count = reader.GetInt32(1)
                });
            }
        }

        // Downloads per day (last 30 days)
        cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT date(CreatedAt) as Day, COUNT(*)
            FROM DownloadJobs
            WHERE UserId = @userId AND CreatedAt > datetime('now', '-30 days')
            GROUP BY date(CreatedAt)
            ORDER BY Day";
        cmd.Parameters.AddWithValue("@userId", userId);

        stats.DownloadsPerDay = new List<DailyDownloadCount>();
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                stats.DownloadsPerDay.Add(new DailyDownloadCount
                {
                    Date = reader.GetString(0),
                    Count = reader.GetInt32(1)
                });
            }
        }

        return stats;
    }

    private DownloadJob MapJob(SqliteDataReader reader)
    {
        return new DownloadJob
        {
            Id = reader.GetString(0),
            UserId = reader.GetString(1),
            Url = reader.GetString(2),
            VideoId = reader.IsDBNull(3) ? null : reader.GetString(3),
            Title = reader.IsDBNull(4) ? null : reader.GetString(4),
            Author = reader.IsDBNull(5) ? null : reader.GetString(5),
            Format = reader.GetString(6),
            Status = Enum.Parse<JobStatus>(reader.GetString(7)),
            Progress = reader.GetInt32(8),
            OriginalFilename = reader.IsDBNull(9) ? null : reader.GetString(9),
            CorrectedFilename = reader.IsDBNull(10) ? null : reader.GetString(10),
            FilePath = reader.IsDBNull(11) ? null : reader.GetString(11),
            FileSizeBytes = reader.GetInt64(12),
            ErrorMessage = reader.IsDBNull(13) ? null : reader.GetString(13),
            RetryCount = reader.GetInt32(14),
            CreatedAt = DateTime.Parse(reader.GetString(15)),
            CompletedAt = reader.IsDBNull(16) ? null : DateTime.Parse(reader.GetString(16)),
            UpdatedAt = DateTime.Parse(reader.GetString(17))
        };
    }

    // Extract YouTube Video ID from URL
    public static string? ExtractVideoId(string url)
    {
        // Handle various YouTube URL formats
        // youtube.com/watch?v=VIDEO_ID
        // youtu.be/VIDEO_ID
        // youtube.com/v/VIDEO_ID
        var patterns = new[]
        {
            @"[?&]v=([a-zA-Z0-9_-]{11})",
            @"youtu\.be/([a-zA-Z0-9_-]{11})",
            @"youtube\.com/v/([a-zA-Z0-9_-]{11})"
        };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(url, pattern);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }
        }

        return null;
    }

    // Whitelist Management Methods
    public async Task<List<WhitelistEntry>> GetWhitelistAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Whitelist ORDER BY AddedAt DESC";
        
        var entries = new List<WhitelistEntry>();
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                entries.Add(MapWhitelistEntry(reader));
            }
        }
        return entries;
    }

    public async Task<WhitelistEntry?> GetWhitelistEntryAsync(string id)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Whitelist WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                return MapWhitelistEntry(reader);
            }
        }
        return null;
    }

    public async Task<WhitelistEntry?> GetWhitelistByUserIdAsync(string userId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Whitelist WHERE UserId = @userId";
        cmd.Parameters.AddWithValue("@userId", userId);
        
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                return MapWhitelistEntry(reader);
            }
        }
        return null;
    }

    public async Task<WhitelistEntry> AddToWhitelistAsync(string userId, string addedBy, bool sendWelcomeEmail)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var entry = new WhitelistEntry
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            AddedBy = addedBy,
            AddedAt = DateTime.UtcNow,
            IsActive = true,
            WelcomeEmailSent = false
        };
        
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Whitelist (Id, UserId, AddedBy, AddedAt, IsActive, WelcomeEmailSent)
            VALUES (@id, @userId, @addedBy, @addedAt, @isActive, @welcomeEmailSent)";
        cmd.Parameters.AddWithValue("@id", entry.Id);
        cmd.Parameters.AddWithValue("@userId", entry.UserId);
        cmd.Parameters.AddWithValue("@addedBy", entry.AddedBy);
        cmd.Parameters.AddWithValue("@addedAt", entry.AddedAt.ToString("O"));
        cmd.Parameters.AddWithValue("@isActive", entry.IsActive ? 1 : 0);
        cmd.Parameters.AddWithValue("@welcomeEmailSent", entry.WelcomeEmailSent ? 1 : 0);
        
        await cmd.ExecuteNonQueryAsync();
        _logger.LogInformation("Added {UserId} to whitelist by {AddedBy}", userId, addedBy);
        
        return entry;
    }

    public async Task UpdateWhitelistStatusAsync(string id, bool isActive)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE Whitelist SET IsActive = @isActive WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@isActive", isActive ? 1 : 0);
        
        await cmd.ExecuteNonQueryAsync();
        _logger.LogInformation("Updated whitelist status for {Id} to {IsActive}", id, isActive);
    }

    public async Task RemoveFromWhitelistAsync(string id)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM Whitelist WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        
        await cmd.ExecuteNonQueryAsync();
        _logger.LogInformation("Removed {Id} from whitelist", id);
    }

    private WhitelistEntry MapWhitelistEntry(SqliteDataReader reader)
    {
        return new WhitelistEntry
        {
            Id = reader.GetString(0),
            UserId = reader.GetString(1),
            AddedBy = reader.GetString(2),
            AddedAt = DateTime.Parse(reader.GetString(3)),
            IsActive = reader.GetInt32(4) == 1,
            WelcomeEmailSent = reader.GetInt32(5) == 1
        };
    }
}

public class DownloadJob
{
    public string Id { get; set; } = "";
    public string UserId { get; set; } = "";
    public string Url { get; set; } = "";
    public string? VideoId { get; set; }
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string Format { get; set; } = "";
    public JobStatus Status { get; set; }
    public int Progress { get; set; }
    public string? OriginalFilename { get; set; }
    public string? CorrectedFilename { get; set; }
    public string? FilePath { get; set; }
    public long FileSizeBytes { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum JobStatus
{
    Pending,
    Downloading,
    Completed,
    Failed
}

// Statistics DTOs
public class GlobalStats
{
    public int TotalDownloads { get; set; }
    public long TotalStorageBytes { get; set; }
    public double TotalStorageMB => TotalStorageBytes / (1024.0 * 1024.0);
    public List<DailyDownloadCount> DownloadsPerDay { get; set; } = new();
    public Dictionary<string, int> StatusCounts { get; set; } = new();
    public int ActiveUsersLast7Days { get; set; }
}

public class DailyDownloadCount
{
    public string Date { get; set; } = "";
    public int Count { get; set; }
}

public class UserStats
{
    public string UserId { get; set; } = "";
    public int TotalDownloads { get; set; }
    public long TotalStorageBytes { get; set; }
    public double TotalStorageMB => TotalStorageBytes / (1024.0 * 1024.0);
    public int CompletedDownloads { get; set; }
    public int FailedDownloads { get; set; }
    public DateTime? LastActive { get; set; }
}

public class UserDetailStats : UserStats
{
    public List<ArtistCount> TopArtists { get; set; } = new();
    public List<DailyDownloadCount> DownloadsPerDay { get; set; } = new();
}

public class ArtistCount
{
    public string Artist { get; set; } = "";
    public int Count { get; set; }
}

// Whitelist DTOs
public class WhitelistEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string AddedBy { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public bool WelcomeEmailSent { get; set; } = false;
}
