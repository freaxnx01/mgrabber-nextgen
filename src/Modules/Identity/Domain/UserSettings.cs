namespace MusicGrabber.Modules.Identity.Domain;

public sealed class UserSettings
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string UserId { get; set; } = string.Empty;

    /// <summary>Default audio format: Mp3, Flac, M4a, WebM.</summary>
    public string DefaultFormat { get; set; } = "Mp3";

    public bool EnableNormalization { get; set; } = true;

    /// <summary>Normalization target in LUFS. Valid range: -20 to -10. Default: -14 (EBU R128).</summary>
    public int NormalizationLufs { get; set; } = -14;

    public bool EmailNotifications { get; set; } = true;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
