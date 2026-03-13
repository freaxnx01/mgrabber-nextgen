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
                Title TEXT,
                Author TEXT,
                Format TEXT NOT NULL,
                Status TEXT NOT NULL,
                Progress INTEGER DEFAULT 0,
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
        ";
        cmd.ExecuteNonQuery();
        _logger.LogInformation("Database initialized");
    }

    public async Task<DownloadJob> CreateJobAsync(string userId, string url, string format, string? title = null, string? author = null)
    {
        var job = new DownloadJob
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = userId,
            Url = url,
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
            INSERT INTO DownloadJobs (Id, UserId, Url, Title, Author, Format, Status, Progress, FileSizeBytes, RetryCount, CreatedAt, UpdatedAt)
            VALUES (@id, @userId, @url, @title, @author, @format, @status, 0, 0, 0, @createdAt, @updatedAt)";
        
        cmd.Parameters.AddWithValue("@id", job.Id);
        cmd.Parameters.AddWithValue("@userId", job.UserId);
        cmd.Parameters.AddWithValue("@url", job.Url);
        cmd.Parameters.AddWithValue("@title", job.Title ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@author", job.Author ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@format", job.Format);
        cmd.Parameters.AddWithValue("@status", job.Status.ToString());
        cmd.Parameters.AddWithValue("@createdAt", job.CreatedAt.ToString("O"));
        cmd.Parameters.AddWithValue("@updatedAt", job.UpdatedAt.ToString("O"));

        await cmd.ExecuteNonQueryAsync();
        return job;
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

    private DownloadJob MapJob(SqliteDataReader reader)
    {
        return new DownloadJob
        {
            Id = reader.GetString(0),
            UserId = reader.GetString(1),
            Url = reader.GetString(2),
            Title = reader.IsDBNull(3) ? null : reader.GetString(3),
            Author = reader.IsDBNull(4) ? null : reader.GetString(4),
            Format = reader.GetString(5),
            Status = Enum.Parse<JobStatus>(reader.GetString(6)),
            Progress = reader.GetInt32(7),
            FilePath = reader.IsDBNull(8) ? null : reader.GetString(8),
            FileSizeBytes = reader.GetInt64(9),
            ErrorMessage = reader.IsDBNull(10) ? null : reader.GetString(10),
            RetryCount = reader.GetInt32(11),
            CreatedAt = DateTime.Parse(reader.GetString(12)),
            CompletedAt = reader.IsDBNull(13) ? null : DateTime.Parse(reader.GetString(13)),
            UpdatedAt = DateTime.Parse(reader.GetString(14))
        };
    }
}

public class DownloadJob
{
    public string Id { get; set; } = "";
    public string UserId { get; set; } = "";
    public string Url { get; set; } = "";
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string Format { get; set; } = "";
    public JobStatus Status { get; set; }
    public int Progress { get; set; }
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
