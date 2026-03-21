namespace MusicGrabber.Modules.Quota.Domain;

public sealed class UserQuota
{
    /// <summary>Default storage quota: 1 GB.</summary>
    public const long DefaultQuotaBytes = 1_073_741_824L;

    public Guid Id { get; set; } = Guid.NewGuid();

    public string UserId { get; set; } = string.Empty;

    /// <summary>Maximum allowed storage in bytes.</summary>
    public long QuotaBytes { get; set; } = DefaultQuotaBytes;

    /// <summary>Currently used storage in bytes (recalculated from file system).</summary>
    public long UsedBytes { get; set; } = 0L;

    /// <summary>Number of files belonging to this user.</summary>
    public int FileCount { get; set; } = 0;

    /// <summary>Current quota threshold level: Normal, Warning, Critical, or Blocked.</summary>
    public string CurrentThreshold { get; set; } = QuotaThreshold.Normal;

    /// <summary>Timestamp of the last threshold notification email sent.</summary>
    public DateTime? LastEmailSentAt { get; set; }

    /// <summary>Threshold level at which the last email was sent (rate-limiting guard).</summary>
    public string? LastEmailThreshold { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Calculates the threshold level based on used vs. total bytes.
    /// Thresholds: Blocked ≥ 100%, Critical ≥ 85%, Warning ≥ 70%, Normal otherwise.
    /// </summary>
    public static string CalculateThreshold(long usedBytes, long quotaBytes)
    {
        if (quotaBytes <= 0)
            return QuotaThreshold.Blocked;

        var percent = (double)usedBytes / quotaBytes * 100;

        return percent >= QuotaThreshold.BlockedPercent ? QuotaThreshold.Blocked
            : percent >= QuotaThreshold.CriticalPercent ? QuotaThreshold.Critical
            : percent >= QuotaThreshold.WarningPercent ? QuotaThreshold.Warning
            : QuotaThreshold.Normal;
    }

    /// <summary>Returns true if there is enough remaining space to store <paramref name="requiredBytes"/>.</summary>
    public bool HasSpaceFor(long requiredBytes)
    {
        return UsedBytes + requiredBytes <= QuotaBytes;
    }
}
