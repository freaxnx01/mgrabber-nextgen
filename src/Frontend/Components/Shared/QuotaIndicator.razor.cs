using Microsoft.AspNetCore.Components;
using MudBlazor;
using MusicGrabber.Shared.DTOs;

namespace MusicGrabber.Frontend.Components.Shared;

public partial class QuotaIndicator
{
    [Parameter]
    public QuotaInfoDto? Quota { get; set; }

    [Parameter]
    public long EstimatedBytes { get; set; }

    private double PercentUsed => Quota is null || Quota.QuotaBytes == 0
        ? 0
        : (double)Quota.UsedBytes / Quota.QuotaBytes * 100;

    private Color GetThresholdColor() => Quota?.CurrentThreshold switch
    {
        "Warning" => Color.Warning,
        "Critical" => Color.Error,
        "Blocked" => Color.Error,
        _ => Color.Info
    };

    private static string FormatBytes(long bytes)
    {
        const double gb = 1024 * 1024 * 1024;
        const double mb = 1024 * 1024;

        return bytes >= gb
            ? $"{bytes / gb:F1} GB"
            : $"{bytes / mb:F0} MB";
    }
}
