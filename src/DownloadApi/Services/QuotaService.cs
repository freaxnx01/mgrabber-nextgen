namespace DownloadApi.Services;

public interface IQuotaService
{
    Task<UserQuotaInfo> GetUserQuotaAsync(string userId);
    Task CheckAndNotifyQuotaAsync(string userId);
    Task<bool> WouldExceedQuotaAsync(string userId, long additionalBytes);
}

public class QuotaService : IQuotaService
{
    private readonly JobRepository _repository;
    private readonly IEmailService _emailService;
    private readonly ILogger<QuotaService> _logger;
    private readonly long _defaultQuotaBytes;
    private readonly double _warningThreshold;
    private readonly double _criticalThreshold;
    private readonly double _blockedThreshold;

    public QuotaService(
        JobRepository repository,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<QuotaService> logger)
    {
        _repository = repository;
        _emailService = emailService;
        _logger = logger;
        
        // Default: 1GB per user, configurable via settings
        _defaultQuotaBytes = long.Parse(configuration["Quota:MaxBytesPerUser"] ?? "1073741824"); // 1GB
        _warningThreshold = double.Parse(configuration["Quota:WarningThreshold"] ?? "0.80"); // 80%
        _criticalThreshold = double.Parse(configuration["Quota:CriticalThreshold"] ?? "0.90"); // 90%
        _blockedThreshold = double.Parse(configuration["Quota:BlockedThreshold"] ?? "0.95"); // 95%
    }

    public async Task<UserQuotaInfo> GetUserQuotaAsync(string userId)
    {
        var jobs = await _repository.GetUserJobsAsync(userId);
        var completedJobs = jobs.Where(j => j.Status == JobStatus.Completed && j.FileSizeBytes > 0);
        
        var usedBytes = completedJobs.Sum(j => j.FileSizeBytes);
        var usedPercent = (double)usedBytes / _defaultQuotaBytes;
        
        return new UserQuotaInfo
        {
            UserId = userId,
            TotalBytesAllowed = _defaultQuotaBytes,
            TotalBytesUsed = usedBytes,
            PercentageUsed = usedPercent * 100,
            RemainingBytes = _defaultQuotaBytes - usedBytes,
            FileCount = completedJobs.Count(),
            Threshold = GetThresholdStatus(usedPercent)
        };
    }

    private QuotaThreshold GetThresholdStatus(double percentage)
    {
        if (percentage >= _blockedThreshold) return QuotaThreshold.Blocked;
        if (percentage >= _criticalThreshold) return QuotaThreshold.Critical;
        if (percentage >= _warningThreshold) return QuotaThreshold.Warning;
        return QuotaThreshold.Normal;
    }

    public async Task<bool> WouldExceedQuotaAsync(string userId, long additionalBytes)
    {
        var quota = await GetUserQuotaAsync(userId);
        return (quota.TotalBytesUsed + additionalBytes) > quota.TotalBytesAllowed;
    }

    public async Task CheckAndNotifyQuotaAsync(string userId)
    {
        var quota = await GetUserQuotaAsync(userId);
        var lastNotification = await GetLastNotificationAsync(userId);
        
        // Check if we should send a notification
        if (quota.Threshold == QuotaThreshold.Normal)
        {
            return; // No notification needed
        }
        
        // Rate limiting: Don't spam - max 1 email per day per threshold
        var hoursSinceLastNotification = (DateTime.UtcNow - lastNotification).TotalHours;
        if (hoursSinceLastNotification < 24)
        {
            _logger.LogDebug("Skipping quota notification for {UserId} - sent {Hours:F1} hours ago", 
                userId, hoursSinceLastNotification);
            return;
        }
        
        try
        {
            var userEmail = await GetUserEmailAsync(userId);
            if (string.IsNullOrEmpty(userEmail))
            {
                _logger.LogWarning("Cannot send quota notification - no email for user {UserId}", userId);
                return;
            }
            
            switch (quota.Threshold)
            {
                case QuotaThreshold.Warning:
                    await _emailService.SendQuotaWarningAsync(userEmail, userId, quota.PercentageUsed);
                    _logger.LogInformation("Sent quota WARNING to {Email} at {Percent:F1}%", 
                        userEmail, quota.PercentageUsed);
                    break;
                    
                case QuotaThreshold.Critical:
                    await _emailService.SendQuotaCriticalAsync(userEmail, userId, quota.PercentageUsed);
                    _logger.LogInformation("Sent quota CRITICAL to {Email} at {Percent:F1}%", 
                        userEmail, quota.PercentageUsed);
                    break;
                    
                case QuotaThreshold.Blocked:
                    await _emailService.SendQuotaBlockedAsync(userEmail, userId, quota.PercentageUsed);
                    _logger.LogInformation("Sent quota BLOCKED to {Email} at {Percent:F1}%", 
                        userEmail, quota.PercentageUsed);
                    break;
            }
            
            await UpdateLastNotificationAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send quota notification to {UserId}", userId);
        }
    }
    
    private async Task<string?> GetUserEmailAsync(string userId)
    {
        // In a real implementation, this would look up the user's email from a user database
        // For now, we'll use the userId as the email (since we use emails as user IDs)
        if (userId.Contains("@"))
        {
            return userId;
        }
        return null;
    }
    
    private async Task<DateTime> GetLastNotificationAsync(string userId)
    {
        // In production, this would be stored in the database
        // For now, return DateTime.MinValue to always allow first notification
        return DateTime.MinValue;
    }
    
    private async Task UpdateLastNotificationAsync(string userId)
    {
        // In production, update database with last notification timestamp
        // For now, no-op
        await Task.CompletedTask;
    }
}

public class UserQuotaInfo
{
    public string UserId { get; set; } = "";
    public long TotalBytesAllowed { get; set; }
    public long TotalBytesUsed { get; set; }
    public double PercentageUsed { get; set; }
    public long RemainingBytes { get; set; }
    public int FileCount { get; set; }
    public QuotaThreshold Threshold { get; set; }
    
    public double UsedMB => TotalBytesUsed / (1024.0 * 1024.0);
    public double TotalMB => TotalBytesAllowed / (1024.0 * 1024.0);
    public double RemainingMB => RemainingBytes / (1024.0 * 1024.0);
}

public enum QuotaThreshold
{
    Normal,
    Warning,    // 80%
    Critical,   // 90%
    Blocked     // 95%
}
