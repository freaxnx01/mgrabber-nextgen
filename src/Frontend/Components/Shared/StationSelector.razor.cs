using Microsoft.AspNetCore.Components;
using MusicGrabber.Modules.Radio.Domain;

namespace MusicGrabber.Frontend.Components.Shared;

public partial class StationSelector
{
    [Parameter]
    public List<RadioStation>? Stations { get; set; }

    [Parameter]
    public string? SelectedStation { get; set; }

    [Parameter]
    public EventCallback<string?> SelectedStationChanged { get; set; }

    [Parameter]
    public bool IsLoading { get; set; }

    private async Task OnStationChanged(string? value)
    {
        SelectedStation = value;
        await SelectedStationChanged.InvokeAsync(value);
    }
}
