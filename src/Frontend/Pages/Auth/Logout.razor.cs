using Microsoft.AspNetCore.Components;

namespace MusicGrabber.Frontend.Pages.Auth;

public partial class Logout
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    protected override void OnInitialized()
    {
        Navigation.NavigateTo("/api/auth/logout", forceLoad: true);
    }
}
