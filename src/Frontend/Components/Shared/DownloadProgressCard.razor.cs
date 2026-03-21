using Microsoft.AspNetCore.Components;
using MudBlazor;
using MusicGrabber.Modules.Download.Domain;

namespace MusicGrabber.Frontend.Components.Shared;

public partial class DownloadProgressCard
{
    [Parameter]
    public List<DownloadJob>? Jobs { get; set; }

    private static Color GetStatusColor(DownloadStatus status) => status switch
    {
        DownloadStatus.Pending => Color.Default,
        DownloadStatus.Downloading => Color.Primary,
        DownloadStatus.Normalizing => Color.Info,
        DownloadStatus.Completed => Color.Success,
        DownloadStatus.Failed => Color.Error,
        _ => Color.Default
    };

    private static Color GetProgressColor(DownloadStatus status) => status switch
    {
        DownloadStatus.Downloading => Color.Primary,
        DownloadStatus.Normalizing => Color.Info,
        DownloadStatus.Completed => Color.Success,
        DownloadStatus.Failed => Color.Error,
        _ => Color.Default
    };

    private static bool IsActive(DownloadStatus status) =>
        status is DownloadStatus.Downloading or DownloadStatus.Normalizing;
}
