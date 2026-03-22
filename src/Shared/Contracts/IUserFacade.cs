using MusicGrabber.Shared.DTOs;

namespace MusicGrabber.Shared.Contracts;

public interface IUserFacade
{
    Task<UserProfileDto?> GetProfileAsync(string userId, CancellationToken ct = default);
}
