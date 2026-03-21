namespace MusicGrabber.Modules.Identity.Domain;

public sealed class WhitelistEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Email address used as the unique identifier for the whitelisted user.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Role assigned to the user: "User" or "Admin".</summary>
    public string Role { get; set; } = "User";

    public string AddedBy { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public bool WelcomeEmailSent { get; set; } = false;

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
