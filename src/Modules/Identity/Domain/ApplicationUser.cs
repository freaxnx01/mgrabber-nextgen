using Microsoft.AspNetCore.Identity;

namespace MusicGrabber.Modules.Identity.Domain;

public sealed class ApplicationUser : IdentityUser
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
