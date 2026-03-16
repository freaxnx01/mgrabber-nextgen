namespace DownloadApi.Services;

public interface IEmailService
{
    Task SendWelcomeEmailAsync(string toEmail, string userName);
    Task SendQuotaWarningAsync(string toEmail, string userName, double usagePercent);
    Task SendQuotaCriticalAsync(string toEmail, string userName, double usagePercent);
    Task SendQuotaBlockedAsync(string toEmail, string userName, double usagePercent);
}
