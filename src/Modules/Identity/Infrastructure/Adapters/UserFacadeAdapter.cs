using MusicGrabber.Modules.Identity.Application.UseCases.GetUserProfile;
using MusicGrabber.Shared.Contracts;
using MusicGrabber.Shared.DTOs;

namespace MusicGrabber.Modules.Identity.Infrastructure.Adapters;

internal sealed class UserFacadeAdapter : IUserFacade
{
    private readonly GetUserProfileHandler _handler;

    public UserFacadeAdapter(GetUserProfileHandler handler)
    {
        _handler = handler;
    }

    public Task<UserProfileDto?> GetProfileAsync(string userId, CancellationToken ct = default) =>
        _handler.HandleAsync(userId, ct);
}
