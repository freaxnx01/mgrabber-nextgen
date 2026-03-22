namespace MusicGrabber.Modules.Quota.Domain;

/// <summary>Quota threshold level constants and percentage boundaries.</summary>
public static class QuotaThreshold
{
    public const string Normal = "Normal";
    public const string Warning = "Warning";
    public const string Critical = "Critical";
    public const string Blocked = "Blocked";

    /// <summary>Usage percentage at which the Warning threshold is triggered (inclusive).</summary>
    public const int WarningPercent = 70;

    /// <summary>Usage percentage at which the Critical threshold is triggered (inclusive).</summary>
    public const int CriticalPercent = 85;

    /// <summary>Usage percentage at which the Blocked threshold is triggered (inclusive).</summary>
    public const int BlockedPercent = 100;
}
