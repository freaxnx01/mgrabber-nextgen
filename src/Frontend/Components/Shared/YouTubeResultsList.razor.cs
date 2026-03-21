using Microsoft.AspNetCore.Components;
using MusicGrabber.Shared.DTOs;

namespace MusicGrabber.Frontend.Components.Shared;

public partial class YouTubeResultsList
{
    [Parameter]
    public List<YouTubeSearchResultDto>? Results { get; set; }

    [Parameter]
    public bool IsLoading { get; set; }

    [Parameter]
    public EventCallback<YouTubeSearchResultDto> OnDownload { get; set; }

    [Parameter]
    public EventCallback<(YouTubeSearchResultDto Result, string Format)> OnDownloadWithFormat { get; set; }

    private const string FormatMp3 = "Mp3";
    private const string FormatFlac = "Flac";
    private const string FormatM4a = "M4a";
    private const string FormatWebM = "WebM";

    private Task DownloadWithFormat(YouTubeSearchResultDto result, string format)
        => OnDownloadWithFormat.InvokeAsync((result, format));
}
