using System.Net;
using System.Net.Mail;

namespace DownloadApi.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private SmtpClient CreateSmtpClient()
    {
        var host = _configuration["Smtp:Host"] ?? throw new InvalidOperationException("SMTP Host not configured");
        var port = int.Parse(_configuration["Smtp:Port"] ?? "587");
        var username = _configuration["Smtp:Username"] ?? throw new InvalidOperationException("SMTP Username not configured");
        var password = _configuration["Smtp:Password"] ?? throw new InvalidOperationException("SMTP Password not configured");
        var enableSsl = bool.Parse(_configuration["Smtp:EnableSsl"] ?? "true");

        return new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(username, password),
            EnableSsl = enableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };
    }

    private string GetFromEmail() => _configuration["Smtp:FromEmail"] ?? "mgrabber@freaxnx01.ch";
    private string GetFromName() => _configuration["Smtp:FromName"] ?? "Music Grabber";

    public async Task SendWelcomeEmailAsync(string toEmail, string userName)
    {
        try
        {
            using var client = CreateSmtpClient();
            
            var message = new MailMessage
            {
                From = new MailAddress(GetFromEmail(), GetFromName()),
                Subject = "🎵 Welcome to Music Grabber!",
                Body = $@"<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #6366f1, #8b5cf6); color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f9fafb; padding: 30px; border-radius: 0 0 8px 8px; }}
        .button {{ display: inline-block; background: #10b981; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; margin: 20px 0; }}
        .footer {{ text-align: center; color: #6b7280; font-size: 12px; margin-top: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎵 Welcome to Music Grabber!</h1>
        </div>
        <div class='content'>
            <p>Hi {userName},</p>
            <p>Great news! You've been granted access to <strong>Music Grabber</strong>.</p>
            <p>You can now search and download your favorite music from YouTube in high-quality audio formats.</p>
            
            <h3>Getting Started:</h3>
            <ul>
                <li>Visit the application</li>
                <li>Search for songs or artists</li>
                <li>Download in MP3 format</li>
                <li>Manage your downloads</li>
            </ul>
            
            <a href='http://192.168.1.124:8086' class='button'>Open Music Grabber</a>
            
            <p>Happy downloading! 🎶</p>
            
            <div class='footer'>
                <p>This email was sent from Music Grabber running on your private network.</p>
            </div>
        </div>
    </div>
</body>
</html>",
                IsBodyHtml = true
            };
            
            message.To.Add(new MailAddress(toEmail));
            
            await client.SendMailAsync(message);
            _logger.LogInformation("Welcome email sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email}", toEmail);
            throw;
        }
    }

    public async Task SendQuotaWarningAsync(string toEmail, string userName, double usagePercent)
    {
        try
        {
            using var client = CreateSmtpClient();
            
            var message = new MailMessage
            {
                From = new MailAddress(GetFromEmail(), GetFromName()),
                Subject = "⚠️ Storage Quota Warning",
                Body = $@"<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #f59e0b; color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f9fafb; padding: 30px; border-radius: 0 0 8px 8px; }}
        .warning {{ color: #f59e0b; font-weight: bold; }}
        .progress {{ background: #e5e7eb; height: 20px; border-radius: 10px; overflow: hidden; margin: 10px 0; }}
        .progress-bar {{ background: #f59e0b; height: 100%; width: {usagePercent}%; }}
        .footer {{ text-align: center; color: #6b7280; font-size: 12px; margin-top: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>⚠️ Storage Quota Warning</h1>
        </div>
        <div class='content'>
            <p>Hi {userName},</p>
            <p class='warning'>Your Music Grabber storage is {usagePercent:F0}% full!</p>
            
            <div class='progress'>
                <div class='progress-bar'></div>
            </div>
            
            <p>To ensure uninterrupted downloads, please:</p>
            <ul>
                <li>Delete old downloads you no longer need</li>
                <li>Download only the songs you really want</li>
            </ul>
            
            <p>Visit your <a href='http://192.168.1.124:8086'>downloads page</a> to manage your files.</p>
            
            <div class='footer'>
                <p>This is an automated message from Music Grabber.</p>
            </div>
        </div>
    </div>
</body>
</html>",
                IsBodyHtml = true
            };
            
            message.To.Add(new MailAddress(toEmail));
            
            await client.SendMailAsync(message);
            _logger.LogInformation("Quota warning sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send quota warning to {Email}", toEmail);
            throw;
        }
    }

    public async Task SendQuotaCriticalAsync(string toEmail, string userName, double usagePercent)
    {
        try
        {
            using var client = CreateSmtpClient();
            
            var message = new MailMessage
            {
                From = new MailAddress(GetFromEmail(), GetFromName()),
                Subject = "🚨 CRITICAL: Storage Quota Almost Full",
                Body = $@"<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #dc2626; color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f9fafb; padding: 30px; border-radius: 0 0 8px 8px; }}
        .critical {{ color: #dc2626; font-weight: bold; font-size: 1.2em; }}
        .progress {{ background: #e5e7eb; height: 20px; border-radius: 10px; overflow: hidden; margin: 10px 0; }}
        .progress-bar {{ background: #dc2626; height: 100%; width: {usagePercent}%; }}
        .footer {{ text-align: center; color: #6b7280; font-size: 12px; margin-top: 20px; }}
        .button {{ display: inline-block; background: #dc2626; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🚨 CRITICAL: Storage Almost Full</h1>
        </div>
        <div class='content'>
            <p>Hi {userName},</p>
            <p class='critical'>Your Music Grabber storage is {usagePercent:F0}% full!</p>
            
            <div class='progress'>
                <div class='progress-bar'></div>
            </div>
            
            <p><strong>Action required:</strong> You must free up space immediately or downloads will be blocked.</p>
            
            <p>Please:</p>
            <ul>
                <li>Delete old downloads you no longer need</li>
                <li>Remove duplicate files</li>
                <li>Download only essential songs</li>
            </ul>
            
            <a href='http://192.168.1.124:8086' class='button'>Manage Downloads Now</a>
            
            <div class='footer'>
                <p>This is an automated message from Music Grabber.</p>
            </div>
        </div>
    </div>
</body>
</html>",
                IsBodyHtml = true
            };
            
            message.To.Add(new MailAddress(toEmail));
            
            await client.SendMailAsync(message);
            _logger.LogInformation("Quota CRITICAL alert sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send quota critical alert to {Email}", toEmail);
            throw;
        }
    }

    public async Task SendQuotaBlockedAsync(string toEmail, string userName, double usagePercent)
    {
        try
        {
            using var client = CreateSmtpClient();
            
            var message = new MailMessage
            {
                From = new MailAddress(GetFromEmail(), GetFromName()),
                Subject = "⛔ Downloads Blocked: Storage Full",
                Body = $@"<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #7f1d1d; color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f9fafb; padding: 30px; border-radius: 0 0 8px 8px; }}
        .blocked {{ color: #7f1d1d; font-weight: bold; font-size: 1.3em; }}
        .progress {{ background: #e5e7eb; height: 20px; border-radius: 10px; overflow: hidden; margin: 10px 0; }}
        .progress-bar {{ background: #7f1d1d; height: 100%; width: {usagePercent}%; }}
        .footer {{ text-align: center; color: #6b7280; font-size: 12px; margin-top: 20px; }}
        .button {{ display: inline-block; background: #7f1d1d; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>⛔ Downloads Blocked</h1>
        </div>
        <div class='content'>
            <p>Hi {userName},</p>
            <p class='blocked'>Your storage is {usagePercent:F0}% full - Downloads are now BLOCKED!</p>
            
            <div class='progress'>
                <div class='progress-bar'></div>
            </div>
            
            <p><strong>To resume downloads, you must free up space.</strong></p>
            
            <p>Actions needed:</p>
            <ol>
                <li>Go to your downloads page</li>
                <li>Select files you no longer need</li>
                <li>Delete them to free up space</li>
                <li>Downloads will automatically resume</li>
            </ol>
            
            <a href='http://192.168.1.124:8086' class='button'>Free Up Space Now</a>
            
            <div class='footer'>
                <p>This is an automated message from Music Grabber.</p>
            </div>
        </div>
    </div>
</body>
</html>",
                IsBodyHtml = true
            };
            
            message.To.Add(new MailAddress(toEmail));
            
            await client.SendMailAsync(message);
            _logger.LogInformation("Quota BLOCKED alert sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send quota blocked alert to {Email}", toEmail);
            throw;
        }
    }
}
