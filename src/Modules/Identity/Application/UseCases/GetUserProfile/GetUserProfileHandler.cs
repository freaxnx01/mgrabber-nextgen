using MusicGrabber.Modules.Identity.Application.Ports.Driven;
using MusicGrabber.Shared.DTOs;

namespace MusicGrabber.Modules.Identity.Application.UseCases.GetUserProfile;

public sealed class GetUserProfileHandler
{
    private readonly IWhitelistRepository _whitelistRepository;

    public GetUserProfileHandler(IWhitelistRepository whitelistRepository)
    {
        _whitelistRepository = whitelistRepository;
    }

    public async Task<UserProfileDto?> HandleAsync(string userId, CancellationToken ct = default)
    {
        var entry = await _whitelistRepository.GetByUserIdAsync(userId, ct);
        if (entry is null)
        {
            return null;
        }

        // TODO: Name and Email are both set to UserId for now.
        // The actual user name comes from OAuth claims at runtime and is not stored
        // in the whitelist. A future enhancement should store display name from the
        // Google OAuth profile or look it up from ApplicationUser.
        return new UserProfileDto(
            UserId: entry.UserId,
            Name: entry.UserId,
            Email: entry.UserId,
            Role: entry.Role,
            CreatedAt: entry.AddedAt);
    }
}
