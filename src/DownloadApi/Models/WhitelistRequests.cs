namespace DownloadApi.Models;

public record AddToWhitelistRequest(string UserId, bool SendWelcomeEmail = false);
public record UpdateWhitelistRequest(bool IsActive);
