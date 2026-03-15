namespace DownloadApi.Models;

public class WhitelistEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string AddedBy { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public bool WelcomeEmailSent { get; set; } = false;
}
